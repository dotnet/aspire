FROM golang:1.22-alpine AS builder
WORKDIR /app
COPY . .
RUN echo "Built at 20251017161737" > /build-info.txt
RUN go build -o qots .

FROM alpine:latest
COPY --from=builder /app/qots /qots
COPY --from=builder /build-info.txt /build-info.txt
ENTRYPOINT ["/qots"]