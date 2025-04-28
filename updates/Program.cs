using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Linq;


namespace update
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Uso: update <version> <url>");
                Environment.Exit(1);
            }

            string version = FormatearVersion(args[0]); // Asegurar formato X.Y.Z
            string link = args[1];

            string carpeta = AppDomain.CurrentDomain.BaseDirectory;
            string archivo = Path.Combine(carpeta, $"msgapp-v{version}.zip");

            Console.WriteLine($"Descargando versión {version} desde {link}...");

            using (WebClient wc = new WebClient())
            {
                wc.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        Console.WriteLine($"Error en la descarga: {e.Error.Message}");
                        Environment.Exit(1);
                    }

                    Console.WriteLine("Extrayendo archivos...");
                    try
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(archivo))
                        {
                            archive.ExtractToDirectory(carpeta, true);
                        }
                        File.Delete(archivo);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error extrayendo archivos: {ex.Message}");
                        Environment.Exit(1);
                    }

                    Console.WriteLine("Actualización completada. Iniciando aplicación...");
                    Process.Start("msgapp.exe");
                    Environment.Exit(0);
                };

                wc.DownloadFileAsync(new Uri(link), archivo);
            }

            Console.ReadLine(); // Para mantener la consola abierta en caso de ejecución manual
        }

        // Método para asegurar que la versión siempre tenga el formato X.Y.Z
        static string FormatearVersion(string version)
        {
            string[] partes = version.Split('.');
            while (partes.Length < 3) // Asegurar que haya al menos 3 partes
            {
                Array.Resize(ref partes, partes.Length + 1);
                partes[partes.Length - 1] = "0";
            }

            return string.Join(".", partes.Take(3)); // Tomar solo los primeros tres valores
        }
    }

    static class ZIP
    {
        // Extrae el ZIP asegurando que los archivos existentes sean reemplazados
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (!string.IsNullOrEmpty(file.Name) && file.Name != "updates.exe")
                    file.ExtractToFile(completeFileName, overwrite);
            }
        }
    }
}
