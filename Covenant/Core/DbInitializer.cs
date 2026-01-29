// Author: Ryan Cobb (@cobbr_io)
// Project: Covenant (https://github.com/cobbr/Covenant)
// License: GNU GPLv3

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Identity;

using YamlDotNet.Serialization;

using Covenant.Models;
using Covenant.Models.Covenant;
using Covenant.Models.Launchers;
using Covenant.Models.Listeners;
using Covenant.Models.Grunts;

namespace Covenant.Core
{
    public static class DbInitializer
    {
        public async static Task Initialize(ICovenantService service, CovenantContext context, RoleManager<IdentityRole> roleManager, ConcurrentDictionary<int, CancellationTokenSource> ListenerCancellationTokens)
        {
            await InitializeListeners(service, context, ListenerCancellationTokens);
            await InitializeImplantTemplates(service, context);
            await InitializeLaunchers(service, context);
            await InitializeTasks(service, context);
            await InitializeRoles(roleManager);
            await InitializeThemes(context);
        }

        public async static Task InitializeImplantTemplates(ICovenantService service, CovenantContext context)
        {
            if (!context.ImplantTemplates.Any())
            {
                var templates = new ImplantTemplate[]
                {
                    new ImplantTemplate
                    {
                        Name = "GruntHTTP",
                        Description = "A Windows implant written in C# that communicates over HTTP.",
                        Language = ImplantLanguage.CSharp,
                        CommType = CommunicationType.HTTP,
                        ImplantDirection = ImplantDirection.Pull,
                        CompatibleDotNetVersions = new List<Common.DotNetVersion> { Common.DotNetVersion.Net35, Common.DotNetVersion.Net40 }
                    },
                    new ImplantTemplate
                    {
                        Name = "GruntSMB",
                        Description = "A Windows implant written in C# that communicates over SMB.",
                        Language = ImplantLanguage.CSharp,
                        CommType = CommunicationType.SMB,
                        ImplantDirection = ImplantDirection.Push,
                        CompatibleDotNetVersions = new List<Common.DotNetVersion> { Common.DotNetVersion.Net35, Common.DotNetVersion.Net40 }
                    },
                    new ImplantTemplate
                    {
                        Name = "GruntBridge",
                        Description = "A customizable implant written in C# that communicates with a custom C2Bridge.",
                        Language = ImplantLanguage.CSharp,
                        CommType = CommunicationType.Bridge,
                        ImplantDirection = ImplantDirection.Push,
                        CompatibleDotNetVersions = new List<Common.DotNetVersion> { Common.DotNetVersion.Net35, Common.DotNetVersion.Net40 }
                    },
                    new ImplantTemplate
                    {
                        Name = "Brute",
                        Description = "A cross-platform implant built on .NET Core 3.1.",
                        Language = ImplantLanguage.CSharp,
                        CommType = CommunicationType.HTTP,
                        ImplantDirection = ImplantDirection.Pull,
                        CompatibleDotNetVersions = new List<Common.DotNetVersion> { Common.DotNetVersion.NetCore31 }
                    }
                };
                templates.ToList().ForEach(t => t.ReadFromDisk());
                await service.CreateImplantTemplates(templates);

                await service.CreateEntities(
                    new ListenerTypeImplantTemplate
                    {
                        ListenerType = await service.GetListenerTypeByName("HTTP"),
                        ImplantTemplate = await service.GetImplantTemplateByName("GruntHTTP")
                    },
                    new ListenerTypeImplantTemplate
                    {
                        ListenerType = await service.GetListenerTypeByName("HTTP"),
                        ImplantTemplate = await service.GetImplantTemplateByName("GruntSMB")
                    },
                    new ListenerTypeImplantTemplate
                    {
                        ListenerType = await service.GetListenerTypeByName("Bridge"),
                        ImplantTemplate = await service.GetImplantTemplateByName("GruntBridge")
                    },
                    new ListenerTypeImplantTemplate
                    {
                        ListenerType = await service.GetListenerTypeByName("Bridge"),
                        ImplantTemplate = await service.GetImplantTemplateByName("GruntSMB")
                    },
                    new ListenerTypeImplantTemplate
                    {
                        ListenerType = await service.GetListenerTypeByName("HTTP"),
                        ImplantTemplate = await service.GetImplantTemplateByName("Brute")
                    }
                );
            }
        }

        public async static Task InitializeListeners(ICovenantService service, CovenantContext context, ConcurrentDictionary<int, CancellationTokenSource> ListenerCancellationTokens)
        {
            if (!context.ListenerTypes.Any())
            {
                await service.CreateEntities<ListenerType>(
                    new ListenerType { Name = "HTTP", Description = "Listens on HTTP protocol." },
                    new ListenerType { Name = "Bridge", Description = "Creates a C2 Bridge for custom listeners." }
                );
            }
            if (!context.Profiles.Any())
            {
                string[] files = Directory.GetFiles(Common.CovenantProfileDirectory, "*.yaml", SearchOption.AllDirectories);
                HttpProfile[] httpProfiles = files.Where(F => F.Contains("HTTP", StringComparison.CurrentCultureIgnoreCase))
                    .Select(F => HttpProfile.Create(F))
                    .ToArray();
                BridgeProfile[] bridgeProfiles = files.Where(F => F.Contains("Bridge", StringComparison.CurrentCultureIgnoreCase))
                    .Select(F => BridgeProfile.Create(F))
                    .ToArray();
                await service.CreateProfiles(httpProfiles);
                await service.CreateProfiles(bridgeProfiles);
            }
            var listeners = (await service.GetListeners()).Where(L => L.Status == ListenerStatus.Active);

            foreach (Listener l in listeners)
            {
                l.Profile = await service.GetProfile(l.ProfileId);
                await service.StartListener(l.Id);
            }
        }

        public async static Task InitializeLaunchers(ICovenantService service, CovenantContext context)
        {
            if (!context.Launchers.Any())
            {
                var launchers = new Launcher[]
                {
                    new BinaryLauncher(),
                    new ShellCodeLauncher(),
                    new PowerShellLauncher(),
                    new MSBuildLauncher(),
                    new InstallUtilLauncher(),
                    new WmicLauncher(),
                    new Regsvr32Launcher(),
                    new MshtaLauncher(),
                    new CscriptLauncher(),
                    new WscriptLauncher()
                };
                await service.CreateEntities(launchers);
            }
        }

        public async static Task InitializeTasks(ICovenantService service, CovenantContext context)
        {
            if (!context.ReferenceAssemblies.Any())
            {
                List<ReferenceAssembly> ReferenceAssemblies = Directory.GetFiles(Common.CovenantAssemblyReferenceNet35Directory).Select(R =>
                {
                    FileInfo info = new FileInfo(R);
                    return new ReferenceAssembly
                    {
                        Name = info.Name,
                        Location = info.FullName.Replace(Common.CovenantAssemblyReferenceDirectory, ""),
                        DotNetVersion = Common.DotNetVersion.Net35
                    };
                }).ToList();
                Directory.GetFiles(Common.CovenantAssemblyReferenceNet40Directory).ToList().ForEach(R =>
                {
                    FileInfo info = new FileInfo(R);
                    ReferenceAssemblies.Add(new ReferenceAssembly
                    {
                        Name = info.Name,
                        Location = info.FullName.Replace(Common.CovenantAssemblyReferenceDirectory, ""),
                        DotNetVersion = Common.DotNetVersion.Net40
                    });
                });
                await service.CreateReferenceAssemblies(ReferenceAssemblies.ToArray());
            }
            if (!context.EmbeddedResources.Any())
            {
                EmbeddedResource[] EmbeddedResources = Directory.GetFiles(Common.CovenantEmbeddedResourcesDirectory).Select(R =>
                {
                    FileInfo info = new FileInfo(R);
                    return new EmbeddedResource
                    {
                        Name = info.Name,
                        Location = info.FullName.Replace(Common.CovenantEmbeddedResourcesDirectory, "")
                    };
                }).ToArray();
                await service.CreateEmbeddedResources(EmbeddedResources);
            }

           
            
            if (!context.GruntTasks.Any())
            {
                List<string> files = Directory.GetFiles(Common.CovenantTaskDirectory)
                    .Where(F => F.EndsWith(".yaml", StringComparison.CurrentCultureIgnoreCase))
                    .ToList();
                IDeserializer deserializer = new DeserializerBuilder().Build();
                foreach (string file in files)
                {
                    Console.WriteLine("Loading yaml file : " + file);
                    string yaml = File.ReadAllText(file);
                    List<SerializedGruntTask> serialized = deserializer.Deserialize<List<SerializedGruntTask>>(yaml);
                    List<GruntTask> tasks = serialized.Select(S => new GruntTask().FromSerializedGruntTask(S)).ToList();
                    foreach (GruntTask task in tasks)
                    {
                        await service.CreateGruntTask(task);
                    }
                }
            }
        }

        public async static Task InitializeRoles(RoleManager<IdentityRole> roleManager)
        {
            List<string> roles = new List<string> { "Administrator", "User", "Listener", "SignalR", "ServiceUser" };
            foreach (string role in roles)
            {
                if (!(await roleManager.RoleExistsAsync(role)))
                {
                    IdentityResult roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public async static Task InitializeThemes(CovenantContext context)
        {
            if (!context.Themes.Any())
            {
                var themes = new List<Theme>
                {
                    new Theme
                    {
                        Name = "Classic Theme",
                        Description = "Covenant's standard, default theme.",

                        BackgroundColor = "#ffffff",
                        BackgroundTextColor = "#212529",

                        PrimaryColor = "#007bff",
                        PrimaryTextColor = "#ffffff",
                        PrimaryHighlightColor = "#0069d9",

                        SecondaryColor = "#6c757d",
                        SecondaryTextColor = "#ffffff",
                        SecondaryHighlightColor = "#545b62",

                        TerminalColor = "#062549",
                        TerminalTextColor = "#ffffff",
                        TerminalHighlightColor = "#17a2b8",
                        TerminalBorderColor = "#17a2b8",

                        NavbarColor = "#343a40",
                        SidebarColor = "#f8f9fa",

                        InputColor = "#ffffff",
                        InputDisabledColor = "#e9ecef",
                        InputTextColor = "#212529",
                        InputHighlightColor = "#0069d9",

                        TextLinksColor = "#007bff",

                        CodeMirrorTheme = CodeMirrorTheme.@default,
                    },
                    new Theme
                    {
                        Name = "Heathen Mode",
                        Description = "A dark theme meant for lawless heathens.",

                        BackgroundColor = "#191919",
                        BackgroundTextColor = "#f5f5f5",

                        PrimaryColor = "#0D56B6",
                        PrimaryTextColor = "#ffffff",
                        PrimaryHighlightColor = "#1D4272",

                        SecondaryColor = "#343a40",
                        SecondaryTextColor = "#ffffff",
                        SecondaryHighlightColor = "#dae0e5",

                        TerminalColor = "#191919",
                        TerminalTextColor = "#ffffff",
                        TerminalHighlightColor = "#3D86E5",
                        TerminalBorderColor = "#ffffff",

                        NavbarColor = "#1D4272",
                        SidebarColor = "#232323",

                        InputColor = "#373737",
                        InputDisabledColor = "#212121",
                        InputTextColor = "#ffffff",
                        InputHighlightColor = "#ffffff",

                        TextLinksColor = "#007bff",

                        CodeMirrorTheme = CodeMirrorTheme.night,
                    }
                };

                await context.Themes.AddRangeAsync(themes);
                await context.SaveChangesAsync();
            }
        }
    }
}