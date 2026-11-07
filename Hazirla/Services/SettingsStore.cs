using System.Globalization;
using System.IO;
using System.Text.Json;
namespace Hazirla.Services;
public sealed record UserSettings(int Mode=0,int Format=0,string Target="2",string? Folder=null);
public sealed class SettingsStore
{
    private readonly string path;
    public SettingsStore(string? path=null) => this.path=path??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Hazirla","settings.json");
    public UserSettings Load()
    {
        try {return Normalize(JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path))??new());}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException){return new();}
    }
    public static UserSettings Normalize(UserSettings value)
    {
        string target=value.Target??"2";
        if(!double.TryParse(target.Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out var mb)||!double.IsFinite(mb)||mb<0.01||mb>100)target="2";
        return value with {Mode=value.Mode is >=0 and <=2?value.Mode:0,Format=value.Format is >=0 and <=2?value.Format:0,Target=target,Folder=Directory.Exists(value.Folder)?value.Folder:null};
    }
    public bool Save(UserSettings value)
    {
        var temporary=path+".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary,JsonSerializer.Serialize(Normalize(value)));
            File.Move(temporary,path,true);return true;
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return false;}
        finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){}}
    }
}
