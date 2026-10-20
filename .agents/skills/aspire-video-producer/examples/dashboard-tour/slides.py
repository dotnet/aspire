"""The slides of the dashboard tour video. assemble.py calls slide() for each step of a "slide" clip.

Everything visual comes from brand.py, so the slides look like those of every other Aspire video.
"""
# brand.py is in the skill's scripts folder; assemble.py puts it on the path.
from brand import W, bullets, draw_code, note_cards, slide_base

TYPES = {"DistributedApplication", "Projects", "VideoDemo_ApiService", "VideoDemo_Web"}

PROBLEMS = [
    ("1", "Many processes", "Projects, containers and executables, each in its own terminal."),
    ("2", "No overview", "Which ones run, which ones failed, which ones depend on each other?"),
]

SOLUTIONS = [
    ("check", "Every resource", "Its state, its health, and its endpoints, in one list."),
    ("check", "Every signal", "Console logs, structured logs, traces, and metrics, side by side."),
]

APPHOST = """
var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.VideoDemo_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.VideoDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
"""


def slide(step_id, chapter_title, label):
    if step_id.startswith("why-"):
        image = slide_base(chapter_title, label, "Many processes, many consoles")
        if step_id == "why-1":
            bullets(image, PROBLEMS + SOLUTIONS, active={0, 1})
        else:
            bullets(image, PROBLEMS + SOLUTIONS, active={2, 3})
        return image

    if step_id == "apphost-1":
        image = slide_base(chapter_title, label, "The AppHost describes the app")
        draw_code(image, (96, 240, 1240, 1030), "AppHost.cs", "csharp", APPHOST, types=TYPES, size=22)
        note_cards(image, [
            ("Add Redis", "a container resource"),
            ("Add Project", "a service resource"),
            ("With Reference", "connects them"),
        ], (1280, 300, W - 96, 1030))
        return image

    raise SystemExit(f"No slide for {step_id}")
