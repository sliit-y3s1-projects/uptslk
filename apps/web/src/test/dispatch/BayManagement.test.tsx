import { describe, expect, it } from "vitest";
import { BayManagementPage } from "@/features/operations/BayManagementPage";
import { buildStaff } from "@/test/factories";
import { apiUrl, http, HttpResponse } from "@/test/handlers";
import { renderWithProviders, screen, waitFor, within } from "@/test/render";
import { server } from "@/test/server";
import { CENTRE_ID, buildTrip } from "./fixtures";

const user = buildStaff("Dispatcher", CENTRE_ID);
const bay = (id: string, code: string, status = "Available", name: string | null = null) => ({ id, centreId: CENTRE_ID, code, name, status });

function mockPage(bays: unknown[], trips: unknown[] = [], extra: Parameters<typeof server.use> = []) {
  // Overrides first: when two handlers match, MSW uses the first one.
  server.use(
    ...extra,
    http.get(apiUrl("/api/v1/centres/centre-1"), () =>
      HttpResponse.json({ id: CENTRE_ID, code: "CMB", name: "Colombo Fort", city: "Colombo", district: "Colombo", status: "Operating", bays, routes: [] }),
    ),
    http.get(apiUrl("/api/v1/centres/centre-1/bays"), () => HttpResponse.json(bays)),
    http.get(apiUrl("/api/v1/trips"), () => HttpResponse.json(trips)),
  );
}

const render = () => renderWithProviders(<BayManagementPage />, { route: "/operations/bays", user });

describe("BayManagementPage", () => {
  it("shows each bay with its live state and the summary counts", async () => {
    mockPage(
      [bay("b1", "B1"), bay("b2", "B2"), bay("b3", "B3"), bay("b4", "B4", "OutOfService")],
      [
        buildTrip({ id: "t1", bayId: "b2", status: "Boarding", routeNumber: "101" }),
        buildTrip({ id: "t2", bayId: "b3", status: "Ready", routeNumber: "202" }),
      ],
    );

    render();

    const grid = (await screen.findByText("Ready for assignment")).closest("div.grid")!;
    expect(within(grid as HTMLElement).getByText("Unavailable for assignment")).toBeInTheDocument();
    expect(within(grid as HTMLElement).getAllByText("Boarding").length).toBe(1);
    expect(within(grid as HTMLElement).getAllByText("Occupied").length).toBe(1);
    expect(screen.getByText("Active bays").nextSibling).toHaveTextContent("3/4");
  });

  it("shows the trip on a bay when it is selected", async () => {
    mockPage([bay("b1", "B1"), bay("b2", "B2")], [buildTrip({ id: "t1", bayId: "b2", status: "Boarding", routeNumber: "101", vehicle: "WP NB-4821" })]);
    const { user: person } = render();

    await person.click((await screen.findAllByText("B2"))[0]);

    expect(await screen.findByText("Route 101 · Colombo - Kandy")).toBeInTheDocument();
  });

  it("shows a message when the centre has no bays", async () => {
    mockPage([]);

    render();

    expect(await screen.findByText("No bays configured for this centre.")).toBeInTheDocument();
  });

  it("sends POST /api/v1/centres/{id}/bays with the new bay and selects it", async () => {
    let bays = [bay("b1", "B1")];
    let body: Record<string, unknown> = {};
    mockPage(bays, [], [
      http.get(apiUrl("/api/v1/centres/centre-1/bays"), () => HttpResponse.json(bays)),
      http.post(apiUrl("/api/v1/centres/centre-1/bays"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        bays = [...bays, bay("b9", "B9", "Available", "Express bay")];
        return HttpResponse.json(bays[1], { status: 201 });
      }),
    ]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: "Create bay" }));
    const dialog = await screen.findByRole("dialog");
    await person.type(within(dialog).getByPlaceholderText("B14"), "B9");
    await person.type(within(dialog).getByPlaceholderText("Express boarding bay"), "Express bay");
    await person.click(within(dialog).getByRole("button", { name: "Create bay" }));

    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(body).toMatchObject({ code: "B9", name: "Express bay", status: "Available" });
    expect(await screen.findByRole("heading", { name: "B9" })).toBeInTheDocument();
  });

  it("shows the API message and keeps the dialog open when the bay code already exists", async () => {
    mockPage([bay("b1", "B1")], [], [
      http.post(apiUrl("/api/v1/centres/centre-1/bays"), () =>
        HttpResponse.json({ error: "A bay with this code already exists at this centre." }, { status: 409 }),
      ),
    ]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: "Create bay" }));
    const dialog = await screen.findByRole("dialog");
    await person.type(within(dialog).getByPlaceholderText("B14"), "B1");
    await person.click(within(dialog).getByRole("button", { name: "Create bay" }));

    expect(await within(dialog).findByText("A bay with this code already exists at this centre.")).toBeInTheDocument();
  });

  it("requires a bay code to create a bay", async () => {
    let posted = false;
    mockPage([bay("b1", "B1")], [], [http.post(apiUrl("/api/v1/centres/centre-1/bays"), () => { posted = true; return HttpResponse.json(bay("b2", "B2")); })]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: "Create bay" }));
    const dialog = await screen.findByRole("dialog");
    await person.click(within(dialog).getByRole("button", { name: "Create bay" }));

    expect(within(dialog).getByPlaceholderText("B14")).toBeInvalid();
    expect(posted).toBe(false);
  });

  it("closes the selected bay with DELETE /api/v1/centres/bays/{id}", async () => {
    let deleted = "";
    mockPage([bay("b1", "B1")], [], [
      http.delete(apiUrl("/api/v1/centres/bays/:id"), ({ params }) => {
        deleted = String(params.id);
        return new HttpResponse(null, { status: 204 });
      }),
    ]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: /Close bay/ }));

    await waitFor(() => expect(deleted).toBe("b1"));
  });

  it("releases a bay with PUT status Available", async () => {
    let body: Record<string, unknown> = {};
    mockPage([bay("b1", "B1", "Occupied", "Main bay")], [], [
      http.put(apiUrl("/api/v1/centres/bays/b1"), async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;
        return new HttpResponse(null, { status: 204 });
      }),
    ]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: /Release bay/ }));

    await waitFor(() => expect(body).toMatchObject({ code: "B1", name: "Main bay", status: "Available" }));
  });

  it("tells the dispatcher when closing a bay is refused", async () => {
    mockPage([bay("b1", "B1")], [], [
      http.delete(apiUrl("/api/v1/centres/bays/b1"), () => HttpResponse.json({ error: "You can only change bays of your own centre." }, { status: 403 })),
    ]);
    const { user: person } = render();

    await person.click(await screen.findByRole("button", { name: /Close bay/ }));

    expect(await screen.findByRole("alert")).toHaveTextContent("You can only change bays of your own centre.");
  });

  it("shows a loading message and an error message when the bays cannot be loaded", async () => {
    mockPage([], [], [http.get(apiUrl("/api/v1/centres/centre-1/bays"), () => HttpResponse.json({ error: "x" }, { status: 500 }))]);

    render();

    expect(screen.getByText("Loading bays...")).toBeInTheDocument();
    expect(await screen.findByText("Failed to load bay data.")).toBeInTheDocument();
  });
});
