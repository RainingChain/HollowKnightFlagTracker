const fs = require('fs');
const { isDeepStrictEqual } = require('util');
const ROOT = "C:\\Users\\Samuel\\source\\repos\\HollowKnightFlagTracker";
const SAVE_DATA_OUTPUT = ROOT + "\\saveData.json";
const FLAG_OUTPUT = ROOT + "\\flag_output.json";

const readJson = () => {
    try {
        return fs.readFileSync(SAVE_DATA_OUTPUT,'utf8').trim();
    } catch(err){
        return null;
    }
};


const GetIgnoredProps = () => {
    var lines = fs.readFileSync("C:\\Users\\Samuel\\source\\repos\\HollowKnightFlagTracker\\ignore_json_changed_properties.txt", 'utf8').split('\n').map(a => a.trim());
    //console.log(lines);
    return new Set(lines);
};

const CompareSaveDatas = (oldJson, newJson) => {
    const list = [];
    list.push(...CompareSaveDatas_GeoInt(oldJson, newJson, true));
    list.push(...CompareSaveDatas_GeoInt(oldJson, newJson, false));
    list.push(...CompareSaveDatas_Bool(oldJson, newJson));

    FindDeepDifferences(
        oldJson.playerData,
        newJson.playerData,
        "playerData",
        GetIgnoredProps(),
        list);

    return list;
}

/*
function areDeeplyEqual(o1, o2) {
  // Check for strict equality on primitives (incl. null)
  if (o1 === o2) return true;
  // Handle type mismatches early
  if (typeof o1 !== typeof o2 || o1 === null || o2 === null) return false;

  // Handle arrays
  if (Array.isArray(o1) && Array.isArray(o2)) {
    if (o1.length !== o2.length) return false;
    for (let i = 0; i < o1.length; i++) {
      if (!areDeeplyEqual(o1[i], o2[i])) return false;
    }
    return true;
  }
  // If one is array, the other isn't (already handled by type check for primitives)
  if (Array.isArray(o1) || Array.isArray(o2)) return false;

  // Handle objects
  const keys1 = Object.keys(o1);
  const keys2 = Object.keys(o2);

  if (keys1.length !== keys2.length) return false;

  for (const key of keys1) {
    if (!keys2.includes(key) || !areDeeplyEqual(o1[key], o2[key])) {
      return false;
    }
  }

  return true;
};
*/

const CompareSaveDatas_GeoInt = (oldJson, newJson, isGeo) => {
    var geoRocksOld = oldJson.sceneData[isGeo ? "geoRocks" : "persistentIntItems"];
    var geoRocksNew = newJson.sceneData[isGeo ? "geoRocks" : "persistentIntItems"];

    const list = [];

    geoRocksNew.forEach(itemNew => {
        if (!itemNew["sceneName"])
            return;

        var alreadyExists = geoRocksOld.some(itemOld => {
            return isDeepStrictEqual(itemOld, itemNew);
        });

        if (!alreadyExists) {
            list.push((isGeo ? "@geo," : "@int,")
                + itemNew["sceneName"] + ","
                + itemNew["id"] + ","
                + itemNew[isGeo ? "hitsLeft" : "value"]);
        }
    });
    return list;
};


const CompareSaveDatas_Bool = (oldJson, newJson) => {
    var geoRocksOld = oldJson.sceneData.persistentBoolItems;
    var geoRocksNew = newJson.sceneData.persistentBoolItems;

    const list = [];

    geoRocksNew.forEach(itemNew => {
        if (!itemNew["sceneName"])
            return;

        var alreadyExists = geoRocksOld.some(itemOld => {
            return isDeepStrictEqual(itemOld, itemNew);
        });

        if (!alreadyExists) {
            list.push("@bool,"
                + itemNew["sceneName"] + ","
                + itemNew["id"] + ","
                + itemNew["activated"]);
        }
    });
    return list;
}


const FindDeepDifferences = (oldValue, newValue, parentPath, ignoreProps, outList) => {
    if (oldValue === newValue)
        return;

    const add = (key, value) => {
        var parentKey = parentPath + "." + key;
        if (ignoreProps.has(parentKey))
            return;
        if (value == "True")
            value = "true";
        if (value == "False")
            value = "false";
        outList.push("@," + parentKey + "," + value);
    };

    for (let key in oldValue){
        const oldToken = oldValue[key];

        if (key in newValue){ // modified existing value
            const newToken = newValue[key];

            if (typeof newToken === 'object' && newToken && typeof oldToken === 'object' && oldToken) {
                FindDeepDifferences(oldToken, newToken, parentPath + "." + key, ignoreProps, outList);
            }
            else if (Array.isArray(newToken) && Array.isArray(oldToken))
            {
                var addedItems = newToken.filter(t => {
                   return oldToken.every(o => !isDeepStrictEqual(t, o));
                });

                if (addedItems.length)
                    add(key, JSON.stringify(addedItems));
            }
            else
            {
                // Primitive value change
                if (!isDeepStrictEqual(newToken, oldToken))
                    add(key, newToken);
            }
        } else {
            // deleted value
            return add(key, 'BUG_deleted'); //bug?
        }
    }

    for (let key in newValue){
        const newToken = newValue[key];

        if (!(key in oldValue)){ // new value
            return add(key, newToken); //bug?
        }
    }
};

let LastSaveDataStr = null;
const update = () => {
    const saveDataStr = readJson();
    if (saveDataStr == null) {
        console.error("saveData == null");
        return;
    }

    if (!LastSaveDataStr){
        LastSaveDataStr = saveDataStr;
        return;
    }

    if (LastSaveDataStr === saveDataStr)
        return;

    const LastSaveData = JSON.parse(LastSaveDataStr);
    const saveData = JSON.parse(saveDataStr);

    LastSaveDataStr = saveDataStr;
    //console.log(LastSaveDataStr, LastSaveData);
    //console.log(saveDataStr, saveData);

    var logs = CompareSaveDatas(LastSaveData, saveData);
    if (logs.length > 0)
    {
        let toAppend = '\r\n\r\n' + new Date().toLocaleString() + '\r\n' + logs.join('\r\n');
        fs.appendFileSync(FLAG_OUTPUT, toAppend);
    }
}




setInterval(() => {
    try {
        update();
    }catch(err){
        console.error(err);
    }
}, 1000);

/*
}*/