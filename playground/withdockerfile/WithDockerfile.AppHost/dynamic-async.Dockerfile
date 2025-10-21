FROM golang:1.22-alpine AS builder
WORKDIR /app
COPY . .
RUN go build -o qots .

FROM alpine:latest
COPY --from=builder /app/qots /qots
ENTRYPOINT ["/qots"]