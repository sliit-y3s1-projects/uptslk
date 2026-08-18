import { Button } from "./components/ui/button";

function App() {
  return (
    <main className="min-h-screen bg-background px-6 py-16">
      <section className="mx-auto flex w-full max-w-4xl flex-col gap-10 rounded-3xl border border-border/70 bg-card p-8 sm:p-12 lg:p-14">
        <div className="max-w-2xl space-y-5">
          <div className="space-y-3">
            <h1 className="text-4xl font-semibold tracking-tight text-foreground sm:text-5xl lg:text-6xl">
              UPTS
            </h1>
            <p className="max-w-xl text-base leading-7 text-muted-foreground sm:text-lg">
              A public transport platform for Sri Lanka, designed to keep
              routes, operations, and rider access in one disciplined system.
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-3">
          <a href="https://github.com/sliit-y3s1-projects/uptslk">
            <Button>Github Repo</Button>
          </a>
        </div>
      </section>
    </main>
  );
}

export default App;
