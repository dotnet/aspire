FROM mcr.microsoft.com/oss/go/microsoft/golang:1.23 AS builder
WORKDIR /app
COPY . .
RUN go build -o qots .
RUN echo "Built at 20260119162134" > /build-info.txt

FROM mcr.microsoft.com/cbl-mariner/base/core:2.0.20260102
COPY --from=builder /app/qots /qots
COPY --from=builder /build-info.txt /build-info.txt
ENTRYPOINT ["/qots"]