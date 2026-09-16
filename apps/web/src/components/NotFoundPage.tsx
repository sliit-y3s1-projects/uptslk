import { ArrowLeft } from "lucide-react";
import { Link } from "react-router";
import { Button } from "@/components/ui/button";

export function NotFoundPage() {
  return <main className="flex min-h-screen items-center justify-center bg-slate-50 px-5"><div className="text-center"><h1 className="text-3xl font-semibold tracking-tight text-slate-950">Page not found</h1><p className="mt-2 text-sm text-slate-500">This page doesn’t exist or may have moved.</p><Button render={<Link to="/" />} variant="link" className="mt-5 text-primary"><ArrowLeft /> Return to booking</Button></div></main>;
}
