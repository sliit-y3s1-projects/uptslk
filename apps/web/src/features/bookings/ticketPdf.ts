import { jsPDF } from "jspdf";
import { QrCode } from "@/features/fares/vendor/qrcodegen";
import { canBoard, departureLabel, type Ticket } from "./ticket";

export function createTicketPdf(ticket: Ticket) {
  if (!canBoard(ticket) || !ticket.qrCode)
    throw new Error("This booking is not valid for boarding.");
  const pdf = new jsPDF({ unit: "mm", format: "a5" });
  pdf.setProperties({
    title: `UPTSLK ticket ${ticket.route}`,
    subject: "Boarding ticket",
  });
  pdf.setFillColor(49, 43, 104);
  pdf.rect(0, 0, 148, 30, "F");
  pdf.setTextColor(255);
  pdf.setFontSize(19);
  pdf.text("UPTSLK", 12, 14);
  pdf.setFontSize(10);
  pdf.text("Seat reservation | Boarding ticket", 12, 23);
  pdf.setTextColor(25);
  pdf.setFontSize(14);
  pdf.text(ticket.route, 12, 41);
  pdf.setFontSize(11);
  const routeLines = pdf.splitTextToSize(
    `${ticket.origin} to ${ticket.destination}`,
    124,
  );
  pdf.text(routeLines, 12, 49);
  let y = 53 + routeLines.length * 5;
  pdf.setFontSize(10);
  for (const line of [
    `Departure: ${departureLabel(ticket.tripTime)} (Sri Lanka)`,
    `Passenger: ${ticket.passengerName}`,
    `Bay: ${ticket.bay}   |   Passengers: ${ticket.passengerCount}`,
    `Fare: LKR ${ticket.fare.toFixed(2)}`,
  ]) {
    const lines = pdf.splitTextToSize(line, 124);
    pdf.text(lines, 12, y);
    y += lines.length * 5 + 3;
  }
  if (y + 75 > 190) {
    pdf.addPage();
    y = 20;
  }
  // Encode the stored API token, exactly as on the mobile boarding pass.
  const qr = QrCode.encodeText(ticket.qrCode, QrCode.Ecc.MEDIUM);
  const moduleSize = 55 / (qr.size + 8);
  const left = (148 - 55) / 2;
  pdf.setFillColor(0, 0, 0);
  for (let row = 0; row < qr.size; row++) {
    for (let column = 0; column < qr.size; column++) {
      if (qr.getModule(column, row))
        pdf.rect(
          left + (column + 4) * moduleSize,
          y + (row + 4) * moduleSize,
          moduleSize,
          moduleSize,
          "F",
        );
    }
  }
  y += 61;
  pdf.setFontSize(8);
  pdf.text(ticket.qrCode, 74, y, { align: "center" });
  pdf.text(
    "Show this QR when boarding. Validity is subject to booking status.",
    74,
    y + 7,
    { align: "center" },
  );
  pdf.text(`Booking: ${ticket.id}`, 12, 198);
  return pdf;
}

export async function downloadTicketPdf(ticket: Ticket) {
  createTicketPdf(ticket).save(`UPTSLK-ticket-${ticket.id}.pdf`);
}
