using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Recorder;

/// <summary>
/// The clips of "An introduction to Aspire": the AppHost in VS Code (served by "code serve-web"), "aspire run" in its
/// terminal and the dashboard it opens, then the Docker Compose deployment.
/// </summary>
public static class Scenes
{
    // The Aspire dashboard of the sample app, on a fixed port, with anonymous access (set in the VS Code terminal profile).
    public static string BaseUrl { get; } = Environment.GetEnvironmentVariable("VIDEO_BASE_URL") ?? "https://localhost:17010";

    public static string LoginUrl { get; } = Environment.GetEnvironmentVariable("VIDEO_LOGIN_URL");

    public static string Root { get; } = Environment.GetEnvironmentVariable("VIDEO_ROOT") ?? throw new InvalidOperationException("Set VIDEO_ROOT to the workspace.");

    public static string StatePath => Path.Combine(Root, "state.json");

    // VS Code, from "code serve-web --port 8123 --without-connection-token", and the sample app it edits.
    private static readonly string s_vsCodeUrl = Environment.GetEnvironmentVariable("VIDEO_VSCODE_URL") ?? "http://localhost:8123";

    private static readonly string s_shared = Environment.GetEnvironmentVariable("VIDEO_SHARED") ?? throw new InvalidOperationException("Set VIDEO_SHARED to the folder of the app.");

    private static string App => Path.Combine(s_shared, "app");

    private const string ApiUrl = "https://localhost:7180";

    // The highlight captions, in the language of the video (VIDEO_LANG).
    private static string Text(string english, string spanish) => Environment.GetEnvironmentVariable("VIDEO_LANG") == "es" ? spanish : english;

    private static readonly (string Name, Func<IBrowser, string, IReadOnlyDictionary<string, double>, Task> Record)[] s_clips =
    [
        ("01-apphost", AppHostAsync),
        ("02-run", RunAsync),
        ("03-deploy", DeployAsync),
    ];

    public static async Task RecordAsync(IBrowser browser, string outputRoot, IReadOnlyDictionary<string, double> durations, string[] names)
    {
        var selected = names is ["all"] ? s_clips : s_clips.Where(clip => names.Contains(clip.Name)).ToArray();

        foreach (var (name, record) in selected)
        {
            Console.WriteLine($"Recording {name}");
            await record(browser, Path.Combine(outputRoot, name), durations);
        }
    }

    private static Task<Scene> NewSceneAsync(IBrowser browser, string directory, IReadOnlyDictionary<string, double> durations, string baseUrl)
        => Scene.StartAsync(browser, Path.GetFileName(directory), baseUrl, StatePath, Path.GetDirectoryName(directory), durations);

    // ---------------------------------------------------------------------------------------------------------------
    // Chapter 1: the AppHost.

    private static async Task AppHostAsync(IBrowser browser, string directory, IReadOnlyDictionary<string, double> durations)
    {
        File.Copy(Path.Combine(s_shared, "apphost.start.mts"), Path.Combine(App, "apphost.mts"), overwrite: true);

        await using var scene = await NewSceneAsync(browser, directory, durations, s_vsCodeUrl);
        await OpenVsCodeAsync(scene);
        await scene.StartCaptureAsync();

        await scene.StepAsync("apphost-1", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "apphost.mts" }), pauseAfter: 1500);
            await HighlightLinesAsync(scene, 5, 5, Text("A Redis cache", "Una caché Redis"));
            await scene.PauseAsync(2200);
            await HighlightLinesAsync(scene, 7, 10, Text("PostgreSQL, a database, and a custom command", "PostgreSQL, una base de datos y un comando"));
        });

        await scene.ClearHighlightsAsync();

        await scene.StepAsync("apphost-2", async () =>
        {
            await HighlightLinesAsync(scene, 12, 15, Text("A Node.js service", "Un servicio Node.js"));
            await scene.PauseAsync(1600);
            await HighlightLinesAsync(scene, 17, 22, Text("A .NET project", "Un proyecto .NET"));
            await scene.PauseAsync(1600);
            await HighlightLinesAsync(scene, 24, 29, Text("A Vite frontend", "Un frontend Vite"));
        });

        await scene.ClearHighlightsAsync();

        await scene.StepAsync("apphost-3", async () =>
        {
            // The end of the catalog's last line: ".withHttpHealthCheck({ path: "/health" })".
            await ClickLineEndAsync(scene, 15);
            await scene.Page.Keyboard.PressAsync("Enter");
            await TypeWithSuggestionAsync(scene, ".withRef", "(cache)");
            await scene.Page.Keyboard.PressAsync("Enter");
            await TypeWithSuggestionAsync(scene, ".wait", "(cache);");
            await ExpectAppHostAsync(File.ReadAllText(Path.Combine(s_shared, "apphost.final.mts")), exact: true);
            });

        await scene.StepAsync("apphost-4", async () =>
        {
            await HighlightLinesAsync(scene, 22, 23, null);
            await scene.PauseAsync(1800);
            await HighlightLinesAsync(scene, 28, 28, null);
        });

        await scene.ClearHighlightsAsync();

        await scene.StepAsync("apphost-5", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "catalog" }), pauseAfter: 500);
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "src" }), pauseAfter: 500);
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "index.ts" }), pauseAfter: 1200);
            await HighlightLinesAsync(scene, 8, 9, Text("No address, port, or password", "Sin dirección, puerto ni contraseña"));
        });

        await scene.ClearHighlightsAsync();
        EnsureFinalAppHost();
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Chapter 2: aspire run, and the dashboard.

    private static async Task RunAsync(IBrowser browser, string directory, IReadOnlyDictionary<string, double> durations)
    {
        EnsureFinalAppHost();
        await AspireAsync("stop");

        await using var scene = await NewSceneAsync(browser, directory, durations, s_vsCodeUrl);
        await OpenVsCodeAsync(scene);
        await scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "apphost.mts" }).ClickAsync();
        await OpenTerminalAsync(scene);
        var vsCode = scene.Page;
        await scene.StartCaptureAsync();

        await scene.StepAsync("run-1", async () =>
        {
            await TypeInTerminalAsync(scene, "aspire run");
            await vsCode.Locator(".xterm-rows").GetByText("localhost:17010").First.WaitForAsync(new() { Timeout = 120_000 });
            await scene.PauseAsync(400);
        });

        await scene.StepAsync("run-2", async () =>
        {
            var link = vsCode.Locator(".xterm-rows span").Filter(new() { HasTextRegex = new Regex("localhost:17010") }).First;
            await scene.MoveToAsync(link);
            await scene.PauseAsync(500);
            var popup = vsCode.Context.WaitForPageAsync();
            // A terminal link opens with Cmd+click on macOS, Ctrl+click elsewhere.
            await vsCode.Keyboard.DownAsync("ControlOrMeta");
            await vsCode.Mouse.ClickAsync((float)(await scene.CenterOfAsync(link)).X, (float)(await scene.CenterOfAsync(link)).Y);
            await vsCode.Keyboard.UpAsync("ControlOrMeta");
            await AcceptOpenLinkAsync(vsCode);
            var dashboard = await popup;
            await scene.SwitchToAsync(dashboard);
            await DismissUnsecuredBannerAsync(scene);
            await WaitForHealthyAsync(scene, 90);
            await scene.HighlightAsync(StateCell(scene, "api"), Text("Running and healthy", "En ejecución y sano"));
        });

        await scene.ClearHighlightsAsync();
        await TrafficAsync(scene);

        await scene.StepAsync("run-3", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Tab, new() { NameRegex = new Regex("^Graph") }), pauseAfter: 1500);
        });

        await scene.StepAsync("run-4", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Link, new() { Name = "Structured logs", Exact = true }), pauseAfter: 1200);
            await scene.TypeAsync(scene.Page.Locator("fluent-text-input").First, "placed", delay: 90);
            await scene.PauseAsync(900);
            await scene.HighlightAsync(scene.Page.GetByRole(AriaRole.Gridcell).Filter(new() { HasTextRegex = new Regex("^\\s*Order \\d+ placed") }).First, Text("A log, with its properties", "Un registro, con sus propiedades"));
        });

        await scene.ClearHighlightsAsync();

        await scene.StepAsync("run-5", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Link, new() { Name = "Traces", Exact = true }), pauseAfter: 1200);
            var row = scene.Page.GetByRole(AriaRole.Row).Filter(new() { HasTextRegex = new Regex("GET /api/products") }).First;
            await scene.ClickAsync(row, pauseAfter: 1500);
        });

        await scene.StepAsync("run-6", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Link, new() { Name = "Metrics", Exact = true }), pauseAfter: 1000);
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Combobox).First, pauseAfter: 600);
            await scene.ClickAsync(scene.Page.Locator("fluent-option").Filter(new() { HasTextRegex = new Regex("^\\s*api\\s*$") }), pauseAfter: 1200);
            await scene.ClickAsync(scene.Page.GetByText("http.server.request.duration", new() { Exact = true }).First, pauseAfter: 1500);
        });

        await scene.StepAsync("run-7", async () =>
        {
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Link, new() { Name = "Resources", Exact = true }), pauseAfter: 800);
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Tab, new() { NameRegex = new Regex("^Resources") }), pauseAfter: 800);
            await scene.ClickAsync(Row(scene, "db").Locator("fluent-button[aria-label='Actions']"), pauseAfter: 700);
            await scene.ClickAsync(scene.Page.GetByRole(AriaRole.Menuitem, new() { Name = "Reset data" }), pauseAfter: 900);
            await scene.ClickAsync(scene.Page.Locator("fluent-dialog fluent-button").Filter(new() { HasTextRegex = new Regex("^\\s*Yes\\s*$") }), pauseAfter: 1500);
        });

        // Stops the app, out of the video (the clip ends with the narration).
        await scene.HoldAsync(1.5);
        await vsCode.BringToFrontAsync();
        await vsCode.Keyboard.PressAsync("Control+c");
        await Task.Delay(4000);
        await AspireAsync("stop");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Chapter 3: Docker Compose.

    private static async Task DeployAsync(IBrowser browser, string directory, IReadOnlyDictionary<string, double> durations)
    {
        EnsureFinalAppHost();

        await using var scene = await NewSceneAsync(browser, directory, durations, s_vsCodeUrl);
        await OpenVsCodeAsync(scene);
        await scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "apphost.mts" }).ClickAsync();
        await Task.Delay(8000);
        await scene.StartCaptureAsync();

        await scene.StepAsync("deploy-1", async () =>
        {
            await ClickLineEndAsync(scene, 3);
            await scene.Page.Keyboard.PressAsync("Enter");
            await scene.Page.Keyboard.PressAsync("Enter");
            await scene.Page.Keyboard.TypeAsync("const compose = await builder", new() { Delay = 80 });
            await scene.Page.Keyboard.PressAsync("Escape");
            await scene.PauseAsync(500);
            await TypeWithSuggestionAsync(scene, ".addDocker", "(\"compose\");");
            await ExpectAppHostAsync("const compose = await builder.addDockerComposeEnvironment(\"compose\");\n");
                await scene.PauseAsync(600);
            await HighlightLinesAsync(scene, 5, 5, Text("A deployment target", "Un destino de despliegue"));
        });

        await scene.ClearHighlightsAsync();

        await scene.StepAsync("deploy-2", async () =>
        {
            await OpenTerminalAsync(scene);
            await RunCommandAsync(scene.Page, "View: Toggle Maximized Panel");
            await TypeInTerminalAsync(scene, "aspire deploy");

            // Waits here, so the last step starts once the deployment is over: the assembly ends the clip with the
            // last step's narration.
            await scene.Page.Locator(".xterm-rows").GetByText("Pipeline succeeded").First.WaitForAsync(new() { Timeout = 240_000 });
        });

        await scene.StepAsync("deploy-3", async () =>
        {
            await scene.PauseAsync(1200);
            await scene.HighlightAsync(scene.Page.Locator(".xterm-rows > div").Filter(new() { HasTextRegex = new Regex("^\\s*frontend: http") }).Last, null);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // VS Code.

    private static async Task OpenVsCodeAsync(Scene scene)
    {
        // The user settings of VS Code in the browser live in the browser's IndexedDB: writes them before the
        // workbench loads. They run the TypeScript extension on the server (in the browser, it runs in a partial mode
        // that doesn't resolve the generated Aspire modules), and turn off Restricted Mode, where it doesn't run.
        await scene.Page.GotoAsync(s_vsCodeUrl + "/manifest.json");
        await scene.Page.EvaluateAsync("""
            async settings => {
              const db = await new Promise((resolve, reject) => {
                const request = indexedDB.open('vscode-web-db', 3);
                request.onupgradeneeded = () => {
                  for (const store of ['vscode-userdata-store', 'vscode-logs-store', 'vscode-filehandles-store'])
                    if (!request.result.objectStoreNames.contains(store)) request.result.createObjectStore(store);
                };
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
              });
              await new Promise((resolve, reject) => {
                const transaction = db.transaction('vscode-userdata-store', 'readwrite');
                transaction.objectStore('vscode-userdata-store').put(new TextEncoder().encode(settings), '/User/settings.json');
                transaction.oncomplete = resolve;
                transaction.onerror = () => reject(transaction.error);
              });
              db.close();
            }
            """, File.ReadAllText(Path.Combine(s_shared, "vscode-user-settings.json")));
        await scene.OpenAsync("/?folder=" + Uri.EscapeDataString(App));
        await scene.Page.Locator(".monaco-workbench").WaitForAsync();
        await scene.Page.GetByRole(AriaRole.Treeitem, new() { Name = "apphost.mts" }).WaitForAsync(new() { Timeout = 60_000 });
        await Task.Delay(2500);

        await scene.Page.Keyboard.PressAsync("Escape");
        await RunCommandAsync(scene.Page, "View: Close All Editors");
        await RunCommandAsync(scene.Page, "Notifications: Clear All Notifications");
        await Task.Delay(800);

        if (Environment.GetEnvironmentVariable("VIDEO_DEBUG_SHOT") is { Length: > 0 } shot)
        {
            await scene.Page.ScreenshotAsync(new() { Path = shot });
        }
    }

    private static async Task RunCommandAsync(IPage page, string command)
    {
        await page.Keyboard.PressAsync("F1");
        await page.Keyboard.TypeAsync(command);
        await Task.Delay(500);
        await page.Keyboard.PressAsync("Enter");
        await Task.Delay(900);
    }

    private static async Task OpenTerminalAsync(Scene scene)
    {
        await scene.Page.Keyboard.PressAsync("Control+Backquote");
        await scene.Page.Locator(".xterm-rows").First.WaitForAsync();
        await Task.Delay(1500);
    }

    private static async Task TypeInTerminalAsync(Scene scene, string command)
    {
        await scene.Page.Keyboard.TypeAsync(command, new() { Delay = 80 });
        await scene.PauseAsync(350);
        await scene.Page.Keyboard.PressAsync("Enter");
    }

    // Types the start of a member, waits for IntelliSense, accepts the suggestion, and types the rest.
    private static async Task TypeWithSuggestionAsync(Scene scene, string start, string rest)
    {
        await scene.Page.Keyboard.TypeAsync(start, new() { Delay = 85 });
        var widget = scene.Page.Locator(".suggest-widget.visible");

        try
        {
            await widget.WaitForAsync(new() { Timeout = 2500 });
        }
        catch (TimeoutException)
        {
            // The TypeScript server wasn't ready when the dot was typed: asks for the suggestions.
            await scene.Page.Keyboard.PressAsync("Control+Space");
            await widget.WaitForAsync(new() { Timeout = 10_000 });
        }

        await scene.PauseAsync(900);
        await scene.Page.Keyboard.PressAsync("Tab");
        await scene.PauseAsync(250);
        await scene.Page.Keyboard.TypeAsync(rest, new() { Delay = 85 });
        await scene.PauseAsync(300);
    }

    // The box of lines first..last of the editor, from the line numbers.
    private static async Task<double[]> LinesBoxAsync(Scene scene, int first, int last)
        => await scene.Page.EvaluateAsync<double[]>("""
            ([first, last]) => {
              const editor = document.querySelector('.editor-instance .monaco-editor');
              const numbers = [...editor.querySelectorAll('.line-numbers')];
              const top = numbers.find(n => n.textContent.trim() === String(first)).getBoundingClientRect();
              const bottom = numbers.find(n => n.textContent.trim() === String(last)).getBoundingClientRect();
              const lines = editor.querySelector('.view-lines').getBoundingClientRect();
              const rows = [...editor.querySelectorAll('.view-line')].filter(l => {
                const r = l.getBoundingClientRect(); return r.top >= top.top - 1 && r.bottom <= bottom.bottom + 1; });
              const width = Math.max(...rows.map(l => l.firstElementChild ? l.firstElementChild.getBoundingClientRect().right : lines.left)) - lines.left;
              return [lines.left, top.top, width, bottom.bottom - top.top];
            }
            """, new[] { first, last });

    private static async Task HighlightLinesAsync(Scene scene, int first, int last, string caption)
    {
        await scene.ClearHighlightsAsync();
        var box = await LinesBoxAsync(scene, first, last);
        await scene.MoveToAsync(box[0] + box[2] + 30, box[1] + box[3] / 2);
        await scene.Page.EvaluateAsync("""
            ({ x, y, w, h, caption }) => {
              const p = 6;
              const ring = document.createElement('div');
              ring.className = 'demo-highlight';
              Object.assign(ring.style, { left: (x - p) + 'px', top: (y - p) + 'px', width: (w + 2 * p) + 'px', height: (h + 2 * p) + 'px' });
              document.documentElement.appendChild(ring);
              if (caption) {
                const label = document.createElement('div');
                label.className = 'demo-caption';
                label.textContent = caption;
                document.documentElement.appendChild(label);
                const right = x + w + 2 * p + 12;
                if (right + label.offsetWidth + 8 < window.innerWidth) {
                  Object.assign(label.style, { left: right + 'px', top: (y + h / 2 - label.offsetHeight / 2) + 'px' });
                } else {
                  Object.assign(label.style, { left: (x - p) + 'px', top: (y - p - label.offsetHeight - 8) + 'px' });
                }
              }
            }
            """, new { x = box[0], y = box[1], w = box[2], h = box[3], caption });
    }

    private static async Task ClickLineEndAsync(Scene scene, int line)
    {
        var box = await LinesBoxAsync(scene, line, line);
        await scene.MoveToAsync(box[0] + box[2] + 40, box[1] + box[3] / 2);
        await scene.PauseAsync(150);
        await scene.Page.Mouse.ClickAsync((float)(box[0] + box[2] + 40), (float)(box[1] + box[3] / 2));
        await scene.Page.Keyboard.PressAsync("End");
        await scene.PauseAsync(300);
    }

    // The dashboard warns that its endpoints accept anonymous access, which the sample app uses so no token shows.
    private static Task DismissUnsecuredBannerAsync(Scene scene)
        => scene.Page.EvaluateAsync("""
            () => {
              const bar = [...document.querySelectorAll('div')].find(d => d.textContent.trim().startsWith('Endpoint is unsecured') && d.querySelector('fluent-button, button'));
              const buttons = bar ? bar.querySelectorAll('fluent-button, button') : [];
              if (buttons.length) buttons[buttons.length - 1].click();
            }
            """);

    // VS Code asks before opening an external link, in some versions.
    private static async Task AcceptOpenLinkAsync(IPage page)
    {
        var open = page.GetByRole(AriaRole.Button, new() { Name = "Open", Exact = true });

        try
        {
            await open.WaitForAsync(new() { Timeout = 1500 });
            await open.ClickAsync();
        }
        catch (TimeoutException)
        {
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // The dashboard and the app.

    private static ILocator Row(Scene scene, string resource)
        => scene.Page.GetByRole(AriaRole.Row).Filter(new() { Has = scene.Page.GetByRole(AriaRole.Gridcell).First.Filter(new() { HasTextRegex = new Regex($"^\\s*{resource}\\b") }) }).First;

    // The columns of the Resources page: Name, State, Start time...
    private static ILocator StateCell(Scene scene, string resource) => Row(scene, resource).GetByRole(AriaRole.Gridcell).Nth(1);

    private static async Task WaitForHealthyAsync(Scene scene, int seconds)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);

        while (DateTime.UtcNow < until)
        {
            var output = await AspireAsync("describe");
            var healthy = Regex.Matches(output, "Running\\s*│\\s*Healthy").Count;

            if (healthy >= 6)
            {
                return;
            }

            await scene.PauseAsync(1000);
        }

        throw new TimeoutException("The resources didn't become healthy.");
    }

    // Places orders and reads products, so the logs, traces, and metrics have data.
    private static async Task TrafficAsync(Scene scene)
    {
        string[] products = ["Hiking Boots", "Headlamp", "Rain Jacket"];

        foreach (var product in products)
        {
            await scene.Context.APIRequest.PostAsync(ApiUrl + "/api/orders", new() { DataObject = new { product, quantity = 1 } });
        }

        for (var i = 0; i < 6; i++)
        {
            await scene.Context.APIRequest.GetAsync(ApiUrl + "/api/products");
            await Task.Delay(300);
        }
    }

    // Checks what was typed, once VS Code saved it.
    private static async Task ExpectAppHostAsync(string text, bool exact = false)
    {
        // Compares with "\n" line endings: the files can have "\r\n" on Windows.
        text = text.ReplaceLineEndings("\n");

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(250);
            var content = File.ReadAllText(Path.Combine(App, "apphost.mts")).ReplaceLineEndings("\n");

            if (exact ? content == text : content.Contains(text))
            {
                return;
            }
        }

        throw new InvalidOperationException("The AppHost doesn't have what was typed:\n" + File.ReadAllText(Path.Combine(App, "apphost.mts")));
    }

    private static void EnsureFinalAppHost()
        => File.Copy(Path.Combine(s_shared, "apphost.final.mts"), Path.Combine(App, "apphost.mts"), overwrite: true);

    private static async Task<string> AspireAsync(string arguments)
    {
        var info = new ProcessStartInfo("aspire", arguments + " --nologo --non-interactive")
        {
            WorkingDirectory = App,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            // The Aspire CLI writes UTF-8 (it sets Console.OutputEncoding): decoding it with the console's code page
            // (on Windows, often not UTF-8) breaks the "│" separators that WaitForHealthyAsync counts.
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        using var process = Process.Start(info)!;

        // Reads both streams at once: a full stderr pipe would block the process while stdout is read.
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(output, error, process.WaitForExitAsync());
        return output.Result;
    }
}
