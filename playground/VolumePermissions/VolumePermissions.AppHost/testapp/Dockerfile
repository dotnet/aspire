FROM mcr.microsoft.com/cbl-mariner/base/core:2.0

# Create users and groups with specific UIDs/GIDs for testing volume permissions
RUN groupadd -g 1001 appgroup && \
    groupadd -g 1002 datagroup && \
    useradd -u 999 -g appgroup -s /bin/bash appuser && \
    useradd -u 1001 -g datagroup -s /bin/bash datauser

# Install basic tools for testing
RUN yum update -y && yum install -y findutils coreutils && yum clean all

# Copy test script
COPY test-permissions.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/test-permissions.sh

# Create mount points
RUN mkdir -p /app/data /app/shared /app/readonly

# Default to running as the app user
USER appuser
WORKDIR /app

# Run the test script that demonstrates volume permissions
CMD ["/usr/local/bin/test-permissions.sh"]