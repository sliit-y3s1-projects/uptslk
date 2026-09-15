# QR encoder

Project Nayuki QR Code generator, MIT licensed. The license is retained in qrcodegen.js.

Source: https://github.com/nayuki/QR-Code-generator/blob/master/typescript-javascript/qrcodegen.ts (retrieved 2026-09-15). Compiled to ES2022 with TypeScript; the only additions are an ESM export. The adjacent declaration describes the API used by TicketQr.

Vendored within the feature to avoid changing the shared package manifest or using a remote QR service. Ticket references stay in the browser.

Upstream TypeScript SHA-256: `1dc03fb5a10e0e2318ea162755bbdb9977ca6ce52cff959e9c9b6deafdccda9c`.
