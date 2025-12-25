// Aspire TypeScript AppHost
// For more information, see: https://learn.microsoft.com/dotnet/aspire

console.log("Aspire TypeScript AppHost starting...");

// TODO: Add your distributed application configuration here
// This is a placeholder - full Aspire integration coming soon!

// Keep the process running
const startTime = Date.now();

console.log("AppHost is running. Press Ctrl+C to stop.");

const intervalId = setInterval(() => {
    const elapsed = Math.floor((Date.now() - startTime) / 1000);
    console.log(`AppHost running for ${elapsed} seconds...`);
}, 5000);

process.on('SIGINT', () => {
    console.log("\nReceived SIGINT. Shutting down gracefully...");
    clearInterval(intervalId);
    process.exit(0);
});

process.on('SIGTERM', () => {
    console.log("\nReceived SIGTERM. Shutting down gracefully...");
    clearInterval(intervalId);
    process.exit(0);
});
