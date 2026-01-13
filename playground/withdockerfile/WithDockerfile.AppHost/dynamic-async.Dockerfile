FROM mcr.microsoft.com/oss/go/microsoft/golang:1.23 AS builder
WORKDIR /app
COPY . .
RUN go build -o qots .

FROM mcr.microsoft.com/cbl-mariner/base/core:2.0.20251206
COPY --from=builder /app/qots /qots
ENTRYPOINT ["/qots"]