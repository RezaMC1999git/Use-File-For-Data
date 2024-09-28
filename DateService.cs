public class DataService
{
    public bool SaveJson<T>(T data, string fileName)
    {
        string path = GetPath<T>(fileName);

        try
        {
            string directoryPath = Path.GetDirectoryName(path);

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            string jsonData = JsonConvert.SerializeObject(data, settings);

            // Do not encrypt in the editor
#if UNITY_EDITOR
            File.WriteAllText(path, jsonData);
#else
        EncryptData(jsonData, path);
#endif
            return true;
        }
        catch (Exception error)
        {
            string errorMessage = $"Error saving JSON data to {path}: {error.Message}\nStack Trace: {error.StackTrace}";
            Debug.LogError(errorMessage);
            return false;
        }
    }

    public T LoadJson<T>(string fileName) where T : new()
    {
        string path = GetPath<T>(fileName);

        if (!File.Exists(path))
        {
            return default;
        }
        try
        {
            //do not decrypt in editor
#if UNITY_EDITOR
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
#else
            string data = DecryptData(path);
            if (data == null)
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(data);
#endif
        }
        catch (Exception error)
        {
            string errorReadFile = "could not read due to: " + error.Message + "\nAnd with stack trace: " + error.StackTrace;
            Debug.LogError(errorReadFile);

            return default;
        }

    }

    private void EncryptData(string data, string path)
    {
        using FileStream stream = File.Create(path);

        using Aes aesProvider = Aes.Create();
        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);
        using ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
        using CryptoStream cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write);

        cryptoStream.Write(Encoding.UTF8.GetBytes(data));
    }
    private string DecryptData(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);

        using Aes aesProvider = Aes.Create();
        aesProvider.Key = Convert.FromBase64String(KEY);
        aesProvider.IV = Convert.FromBase64String(IV);

        using ICryptoTransform cryptoTransform = aesProvider.CreateDecryptor(aesProvider.Key, aesProvider.IV);
        using MemoryStream decryptionStream = new MemoryStream(fileBytes);
        using CryptoStream cryptoStream = new CryptoStream(decryptionStream, cryptoTransform, CryptoStreamMode.Read);

        try
        {
            using StreamReader reader = new StreamReader(cryptoStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }
    private string GetPath<T>(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        return Path.Combine(Application.persistentDataPath, fileName + ".json");
    }
}