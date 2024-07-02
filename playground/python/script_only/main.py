import logging

# Reset the logging configuration to a sensible default.
logging.basicConfig()
logging.getLogger().setLevel(logging.NOTSET)

# Write a basic log message.
logging.getLogger(__name__).info("Hello world!")
