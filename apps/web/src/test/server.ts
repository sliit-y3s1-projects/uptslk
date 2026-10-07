import { setupServer } from "msw/node";
import { defaultHandlers } from "./handlers";

/** Mock API used by every test. Add per-test responses with `server.use(...)`; they are reset after each test. */
export const server = setupServer(...defaultHandlers);
