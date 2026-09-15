export declare class QrCode {
  static readonly Ecc: { readonly MEDIUM: object };
  static encodeText(text: string, ecl: object): QrCode;
  readonly size: number;
  getModule(x: number, y: number): boolean;
}
