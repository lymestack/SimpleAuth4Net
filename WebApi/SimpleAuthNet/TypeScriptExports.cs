using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Config;
using SimpleAuthNet.Models.ViewModels;
using TypeGen.Core.SpecGeneration;

namespace SimpleAuthNet;

/// <summary>
/// This file exports C# classes into TypeScript classes of the Angular application.
/// Options are configured in the tgconfig.json file.
/// </summary>
public class TypeScriptExports : GenerationSpec
{
    public TypeScriptExports()
    {
        AddClass<AppUser>();
        AddClass<AppRole>();
        AddClass<AppConfig>();
        AddClass<LoginModel>();
        AddClass<LoginWithGoogleModel>();
        AddClass<RegisterModel>();
    }
}