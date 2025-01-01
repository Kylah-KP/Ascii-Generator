using System.Diagnostics;
using System.Drawing;
using System.Text;
using NAudio.Wave;

class Program
{
    static void Main(string[] args)
    {
        int targetWith = Console.WindowWidth - 1;
        int targetHeight = Console.WindowHeight - 2;

        Console.WriteLine("Choose an option");
        Console.WriteLine("[1] Rendering video to ASCII");
        Console.WriteLine("[2] Rendering image to ASCII");
        Console.Write("Enter your choice: ");

        var choice = Console.ReadLine();

        Console.Clear();

        if (choice == "1")
        {
            string InputFileName = InOutManager.GetInputFileName(args);

            InOutManager.CleanUpDirectories();
            FFmpegManager.ExtractFrames(InputFileName, targetWith, targetHeight);
            FFmpegManager.ExtractAudio(InputFileName);
            var frames = AsciiConverter.ConvertFramesToAscii(targetWith, targetHeight);
            VideoPlayer.AsciiVideoPlayer(frames);
        }
        else if (choice == "2")
        {
            string InputFileName = InOutManager.GetInputFileName(args);

            var image = AsciiConverter.ConvertImageToAscii(InputFileName, targetWith);
            Console.WriteLine(image);

            InOutManager.ImageSave(image);
        }
    }
}

class InOutManager
{
    public const string FramesDirectory = "cvf\\frames\\";
    public const string AudioFilePath = "cvf\\audio.wav";

    public static string GetInputFileName(string[] args)
    {
        if (args.Length > 0)
            return args[0];

        Console.Write("Input File: ");
        var input = Console.ReadLine();
        Console.Clear();
        return input?.Replace("\"", "") ?? string.Empty;
    }

    public static void CleanUpDirectories()
    {
        if (Directory.Exists("cvf"))
        {
            if (Directory.Exists(FramesDirectory))
            {
                Directory.Delete(FramesDirectory, true);
            }
            if (File.Exists(AudioFilePath))
            {
                File.Delete(AudioFilePath);
            }
        }
        Directory.CreateDirectory(FramesDirectory);
    }

    public static void ImageSave(string image)
    {
        Console.Write("Do you want to save the result to a file? (y/n) (Default = n): ");
        var saveChoice = Console.ReadLine();
        if (saveChoice?.ToLower() == "y")
        {
            string outputDirectory = "images";
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            Console.Write("Enter name of output file: ");
            var fileName = Console.ReadLine();

            string outputFileName = Path.Combine(outputDirectory, fileName + ".txt");
            File.WriteAllText(outputFileName, image);
            Console.WriteLine($"ASCII art saved to {outputFileName}");
        }
    }

    public static void DisplayProgress(int currentFrame, int totalFrames)
    {
        int barLength = 50;
        int percentage = (int)((currentFrame / (float)totalFrames) * 100);
        int filledLength = (percentage * barLength) / 100;

        string progressBar = new string('#', filledLength) + new string('-', barLength - filledLength);

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write($"-> [PROGRESS]  Converting to ASCII    [{progressBar}] {percentage}%  ");
    }

    public static void WarringMessage(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.BackgroundColor = ConsoleColor.White;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}

class FFmpegManager
{
    private const string FFmpegPath = "ffmpeg.exe";

    public static void ExtractFrames(string inputFile, int width, int height)
    {
        RunFFmpegProcess($"-i \"{inputFile}\" -vf scale={width}:{height} {InOutManager.FramesDirectory}%0d.bmp");
    }

    public static void ExtractAudio(string inputFile)
    {
        RunFFmpegProcess($"-i \"{inputFile}\" {InOutManager.AudioFilePath}");
    }

    private static void RunFFmpegProcess(string arguments)
    {
        using var process = new Process();

        process.StartInfo.FileName = FFmpegPath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        Console.WriteLine($"Waiting: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.Start();
        process.WaitForExit();
    }
}

class AsciiConverter
{
    private const string BrightnessLevels = " .-+*wvGHM#&%";

    public static string ConvertImageToAscii(string imagePath, int targetWidth)
    {
        using Bitmap originalBitmap = new(imagePath);

        double aspectRatio = originalBitmap.Height / (double)originalBitmap.Width; // Calculate target height based on initial scale
        int targetHeight = (int)(aspectRatio * targetWidth * 0.45); // 0.45 to adjust console character scaling

        using Bitmap resizedBitmap = new(originalBitmap, new Size(targetWidth, targetHeight));
        StringBuilder asciiBuilder = new();

        for (int y = 0; y < resizedBitmap.Height; y++)
        {
            for (int x = 0; x < resizedBitmap.Width; x++)
            {
                Color pixelColor = resizedBitmap.GetPixel(x, y);
                // Map pixel brightness to ASCII character array
                int brightnessIndex = (int)(pixelColor.GetBrightness() * BrightnessLevels.Length);
                brightnessIndex = Math.Clamp(brightnessIndex, 0, BrightnessLevels.Length - 1);
                asciiBuilder.Append(BrightnessLevels[brightnessIndex]);
            }
            asciiBuilder.AppendLine();
        }

        return asciiBuilder.ToString();
    }

    public static List<string> ConvertFramesToAscii(int width, int height)
    {
        var frames = new List<string>();
        int frameCount = Directory.GetFiles(InOutManager.FramesDirectory, "*.bmp").Length;

        InOutManager.WarringMessage("[NOTE] Do not resize the console window while the process running!");

        for (int frameIndex = 1; frameIndex <= frameCount; frameIndex++)
        {
            string fileName = $"{InOutManager.FramesDirectory}{frameIndex}.bmp";
            if (!File.Exists(fileName))
                break;

            frames.Add(ConvertFrameToAscii(fileName, width, height));
            InOutManager.DisplayProgress(frameIndex, frameCount);
        }
        return frames;
    }

    private static string ConvertFrameToAscii(string fileName, int width, int height)
    {
        StringBuilder frameBuilder = new();
        using (Bitmap bitmap = new(fileName))
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    int brightnessIndex = (int)(bitmap.GetPixel(x, y).GetBrightness() * BrightnessLevels.Length);
                    brightnessIndex = Math.Clamp(brightnessIndex, 0, BrightnessLevels.Length - 1);
                    frameBuilder.Append(BrightnessLevels[brightnessIndex]);
                }
                frameBuilder.AppendLine();
            }
        }
        return frameBuilder.ToString();
    }
}

class VideoPlayer
{

    public static void AsciiVideoPlayer(List<string> frames)
    {
        Console.Clear();
        Console.WriteLine("============Console============");
        Console.WriteLine("|         Enter - Run         |");
        Console.WriteLine("|    Space - Pause / Play     |");
        Console.WriteLine("|         Esc - Stop          |");
        Console.WriteLine("===============================");

        while (true)
        {
            var waitKey = Console.ReadKey(true).Key;
            if (waitKey == ConsoleKey.Enter || waitKey == ConsoleKey.Y)
            {
                PlayAsciiVideo(frames);
                Console.WriteLine("Done!");
                Console.Write("[INFO] Do you want to play the video again? (y/n) (Default = y): ");
            }
            else if (waitKey == ConsoleKey.Escape || waitKey == ConsoleKey.N)
                break;
        }
        return;
    }

    private static void PlayAsciiVideo(List<string> frames)
    {
        using var reader = new AudioFileReader(InOutManager.AudioFilePath);
        using var waveOut = new WaveOutEvent();

        waveOut.Init(reader);
        Console.Clear();
        Console.CursorVisible = false;
        waveOut.Play();

        bool isPlaying = true;

        while (true)
        {
            int frameIndex = (int)((waveOut.GetPosition() / (float)reader.Length) * frames.Count);
            if (frameIndex >= frames.Count)
                break;

            Console.SetCursorPosition(0, 0);
            Console.Write(frames[frameIndex]);

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Spacebar)
                {
                    if (isPlaying)
                    {
                        waveOut.Pause();
                        isPlaying = false;
                    }
                    else
                    {
                        waveOut.Play();
                        isPlaying = true;
                    }
                }
                else if (key == ConsoleKey.Escape)
                {
                    waveOut.Stop();
                    break;
                }
            }
            Task.Delay(30);
        }
        Console.CursorVisible = true;
    }
}