import { QrCode } from "../vendor/qrcodegen";
export function TicketQr({ value }: { value: string }) {
  const qr = QrCode.encodeText(value, QrCode.Ecc.MEDIUM);
  const path: string[] = [];
  for (let y = 0; y < qr.size; y++) {
    for (let x = 0; x < qr.size; x++) {
      if (qr.getModule(x, y)) path.push(`M${x + 4},${y + 4}h1v1h-1z`);
    }
  }
  return (
    <svg
      role="img"
      aria-label={`Ticket QR code ${value}`}
      viewBox={`0 0 ${qr.size + 8} ${qr.size + 8}`}
      className="w-full max-w-44"
      shapeRendering="crispEdges"
    >
      <rect width="100%" height="100%" fill="white" />
      <path d={path.join(" ")} fill="black" />
    </svg>
  );
}
