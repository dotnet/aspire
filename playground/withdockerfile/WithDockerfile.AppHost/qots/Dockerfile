# Stage 1: Build the Go program
ARG GO_VERSION=1.16
FROM golang:${GO_VERSION} AS builder
WORKDIR /app
COPY . .
RUN go build qots.go

# Stage 2: Run the Go program
ARG GO_VERSION
FROM golang:${GO_VERSION}
WORKDIR /app
RUN --mount=type=secret,id=SECRET_ENV cp /run/secrets/SECRET_ENV /app/SECRET_ENV
RUN --mount=type=secret,id=SECRET_FILE  cp /run/secrets/SECRET_FILE /app/SECRET_FILE
COPY --from=builder /app/qots .
CMD ["./qots"]
