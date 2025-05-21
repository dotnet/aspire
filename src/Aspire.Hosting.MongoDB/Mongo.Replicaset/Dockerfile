ARG IMAGE_NAME
ARG IMAGE_TAG

FROM $IMAGE_NAME:$IMAGE_TAG

# escape=`
COPY --chmod=0755 <<EOF /aspire/replicaset-init.sh
#! /usr/bin/env bash

if [ -z "\$MONGO_REPLICASET_NAME" ]
then
    echo "No replicaset defined - exiting init"
    exit 0
fi

# Wait for the port to be enabled
while ! 2> /dev/null > "/dev/tcp/0.0.0.0/\$MONGO_PORT"; do sleep 1; done;

# Loop until the replica set initialization is successful
max_attempts=30
attempt=1
success=false

while [ \$attempt -le \$max_attempts ] && [ "\$success" = false ]; do
    echo "Attempt \$attempt of \$max_attempts to initialize replica set..."
    output=\$(mongosh --port "\$MONGO_PORT" --username "\$MONGO_INITDB_ROOT_USERNAME" --password "\$MONGO_INITDB_ROOT_PASSWORD" --eval "rs.initiate({ _id: '\$MONGO_REPLICASET_NAME', members:[{_id:0,host:'\$MONGO_HOSTANDPORT'}]})")
    
    if echo "\$output" | grep -q "{ ok: 1 }"; then
        success=true
        echo "Replica set initialization successful!"
    else
        echo "Initialization attempt failed, waiting before retry..."
        sleep 1
        attempt=\$((attempt + 1))
    fi
done

if [ "\$success" = false ]; then
    echo "Failed to initialize replica set after \$max_attempts attempts"
    exit 1
fi

EOF

# escape=`
COPY --chmod=0755 <<EOF /aspire/entrypoint.sh
#! /usr/bin/env bash

if [ -n "\$MONGO_KEYFILE_PATH" ]
then
    echo "Creating a key file at \$MONGO_KEYFILE_PATH"
    openssl rand -base64 756 > \$MONGO_KEYFILE_PATH
    chmod 400 \$MONGO_KEYFILE_PATH
    chown 999:999 \$MONGO_KEYFILE_PATH
fi

exec docker-entrypoint.sh \$@ &
/aspire/replicaset-init.sh &

wait

EOF

ENTRYPOINT ["/aspire/entrypoint.sh"]
