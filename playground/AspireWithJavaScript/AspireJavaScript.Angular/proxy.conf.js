module.exports = {
  "/api": {
    target:
      process.env["WEATHERAPI_HTTPS"] || process.env["WEATHERAPI_HTTP"],
    secure: process.env["NODE_ENV"] !== "development"
  },
};
