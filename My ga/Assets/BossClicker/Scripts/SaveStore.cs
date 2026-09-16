using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BossClicker
{
    public sealed class SaveStore
    {
        readonly string path;
        readonly GameBalance balance;
        bool recoveredBackup;

#if UNITY_WEBGL && !UNITY_EDITOR
        const string WebKey = "lootshot-save-v4-test";
        const string WebBackupKey = "lootshot-save-v4-test-backup";
#endif

        public SaveStore(string path, GameBalance balance)
        { this.path = Path.GetFullPath(path); this.balance = balance; }

        public bool TryLoad(out SaveData data, out string message)
        {
            data = null;
            message = "";
            if (!Exists(path) && !Exists(path + ".bak"))
            {
                data = new SaveData(balance.weapons.Length);
                return true;
            }
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!Exists(candidate)) continue;
                try
                {
                    var loaded = JsonUtility.FromJson<SaveData>(Read(candidate));
                    if (loaded == null || !loaded.IsValid(balance))
                        throw new InvalidDataException("存档字段不完整或超出范围。");
                    data = loaded;
                    recoveredBackup = candidate != path;
                    message = recoveredBackup ? "已从上一份备份恢复进度。" : "";
                    return true;
                }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException ||
                                           e is UnauthorizedAccessException)
                { message = "无法读取存档，原文件已保留：" + e.Message; }
            }
            return false;
        }

        public bool TrySave(SaveData data, out string message)
        {
            message = "";
            if (data == null || !data.IsValid(balance))
            {
                message = "存档数据无效，尚未保存。";
                return false;
            }
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string json = JsonUtility.ToJson(data, true);
                if (PlayerPrefs.HasKey(WebKey) && !recoveredBackup)
                    PlayerPrefs.SetString(WebBackupKey, PlayerPrefs.GetString(WebKey));
                PlayerPrefs.SetString(WebKey, json);
                PlayerPrefs.Save();
#else
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temp, path, recoveredBackup ? null : path + ".bak");
                else
                    File.Move(temp, path);
#endif
                recoveredBackup = false;
                return true;
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException ||
                                       e is UnauthorizedAccessException)
            {
                message = "进度尚未保存，请重试：" + e.Message;
                return false;
            }
        }

        public bool TryReset(out SaveData data, out string message)
        {
            data = new SaveData(balance.weapons.Length);
            message = "";
            try
            {
                string json = JsonUtility.ToJson(data, true);
#if UNITY_WEBGL && !UNITY_EDITOR
                PlayerPrefs.SetString(WebKey, json);
                PlayerPrefs.SetString(WebBackupKey, json);
                PlayerPrefs.Save();
#else
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json, new UTF8Encoding(false));
                File.WriteAllText(path + ".bak", json, new UTF8Encoding(false));
#endif
                recoveredBackup = false;
                return true;
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException ||
                                       e is ArgumentException || e is UnauthorizedAccessException)
            {
                message = "无法重置进度，请重试：" + e.Message;
                data = null;
                return false;
            }
        }

        bool Exists(string candidate)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.HasKey(candidate == path ? WebKey : WebBackupKey);
#else
            return File.Exists(candidate);
#endif
        }

        string Read(string candidate)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return PlayerPrefs.GetString(candidate == path ? WebKey : WebBackupKey);
#else
            return File.ReadAllText(candidate);
#endif
        }
    }
}
