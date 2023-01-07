using System;
using System.IO;

namespace TestChromeExtension.IO;

class DisposableFile : IDisposable
{
    public DisposableFile(string path)
    {
        Path = path;
    }


    public string Path { get; }

    public void Dispose()
    {
        if (File.Exists(Path))
            File.Delete(Path);

        if (Directory.Exists(Path))
            Directory.Delete(Path, true);
    }
}