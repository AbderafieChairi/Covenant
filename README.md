to start the project

```bash
dotnet run --project Covenant/Covenant.csproj
```


Submodules Path 


Covenant/Data/ReferenceSourceLibraries



now remove the use of all of this :
var ru = await service.GetReferenceSourceLibraryByName("Rubeus");
var se = await service.GetReferenceSourceLibraryByName("Seatbelt");
var sd = await service.GetReferenceSourceLibraryByName("SharpDPAPI");
var sdu = await service.GetReferenceSourceLibraryByName("SharpDump");
var su = await service.GetReferenceSourceLibraryByName("SharpUp");
var sw = await service.GetReferenceSourceLibraryByName("SharpWMI");
var sc = await service.GetReferenceSourceLibraryByName("SharpSC");