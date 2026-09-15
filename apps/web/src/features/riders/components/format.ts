export const money = (amount: number) =>
  new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR" }).format(
    amount,
  );
export const dateTime = (value: string) =>
  new Date(value).toLocaleString("en-LK", {
    dateStyle: "medium",
    timeStyle: "short",
  });
