# Stage 1: Build the Go program
ARG GO_VERSION=1.16
FROM golang:${GO_VERSION} AS builder
WORKDIR /app
COPY . .
RUN go build qots.go

# Stage 2: Run the Go program
FROM mcr.microsoft.com/cbl-mariner/base/core:2.0
WORKDIR /app
RUN --mount=type=secret,id=SECRET_ASENV cp /run/secrets/SECRET_ASENV /app/SECRET_ASENV
COPY --from=builder /app/qots .
CMD ["./qots"]
