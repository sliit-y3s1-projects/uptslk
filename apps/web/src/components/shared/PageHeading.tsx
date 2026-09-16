import type { ReactNode } from "react";
import { Trash2 } from "lucide-react";
import { useLocation, useNavigate } from "react-router";
import { Button } from "@/components/ui/button";
import { useCloseCentre } from "@/features/centres/hooks/useCentres";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { useState } from "react";

export function PageHeading({ title, description, action }: { title: string; description: string; action?: ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  const closeCentre = useCloseCentre();
  const [deleteOpen, setDeleteOpen] = useState(false);
  const match = location.pathname.match(/^\/admin\/centres\/([^/]+)$/);
  const centreId = match?.[1];
  function deleteCentre() {
    if (!centreId) return;
    closeCentre.mutate(centreId, { onSuccess: () => navigate("/admin/centres") });
  }
  return <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between"><div><h1 className="text-xl font-semibold tracking-tight">{title}</h1><p className="mt-1 text-sm text-muted-foreground">{description}</p></div><div className="flex items-center gap-2">{action}{centreId && <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}><Button variant="outline" className="text-destructive" onClick={() => setDeleteOpen(true)} disabled={closeCentre.isPending}><Trash2 /> Delete centre</Button><AlertDialogContent><AlertDialogHeader><AlertDialogTitle>Delete this centre?</AlertDialogTitle><AlertDialogDescription>This permanently removes the centre record. Related operational data may no longer be available.</AlertDialogDescription></AlertDialogHeader><AlertDialogFooter><AlertDialogCancel>Cancel</AlertDialogCancel><AlertDialogAction className="bg-destructive text-white hover:bg-destructive/90" onClick={deleteCentre}>Delete centre</AlertDialogAction></AlertDialogFooter></AlertDialogContent></AlertDialog>}</div></div>;
}
