using STEAreaAssemblerVersion;
foreach (var path in Directory.GetFiles(args[0],"*.bin")) {
    new Heightmap().Read(Path.GetFileNameWithoutExtension(path),File.ReadAllBytes(path),args[1],"bounded April source",null);
}
