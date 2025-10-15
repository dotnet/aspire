const WEATHER_API_URL = process.env.services__weatherapi__https__0 || process.env.services__weatherapi__http__0;

console.log('Minimal Node.js App Started!');
console.log('================================');
console.log(`Weather API URL: ${WEATHER_API_URL || 'Not configured'}`);

async function fetchWeather() {
  if (!WEATHER_API_URL) {
    console.log('⚠️  Weather API URL not configured');
    return;
  }

  try {
    console.log('\n🌤️  Fetching weather forecast...');
    const apiUrl = `${WEATHER_API_URL}/weatherforecast`;
    const response = await fetch(apiUrl);

    if (!response.ok) {
      throw new Error(`Weather API returned ${response.status}`);
    }

    const data = await response.json();
    console.log('\n📊 Weather Forecast:');
    console.log('─────────────────────');

    data.forEach(item => {
      console.log(`\n📅 ${item.date}`);
      console.log(`   Temperature: ${item.temperatureC}°C (${item.temperatureF}°F)`);
      console.log(`   Summary: ${item.summary}`);
    });

    console.log('\n✅ Successfully fetched weather data!');
  } catch (error) {
    console.error('\n❌ Error fetching weather:', error.message);
  }
}

// Fetch weather immediately
fetchWeather();

// Keep the process running
setInterval(() => {
  console.log(`\n[${new Date().toLocaleTimeString()}] App is running...`);
}, 30000); // Log every 30 seconds
