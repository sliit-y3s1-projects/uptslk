import { ArrowLeft, BusFront, MapPinned, Route } from "lucide-react";
import { Link, useParams } from "react-router";
import { Button } from "@/components/ui/button";
import { CentreConsolePage } from "@/features/centres/CentreConsolePage";
import { RoutesPage, RouteDetailPage } from "@/features/network/RoutesPage";
import {
  VehiclesPage,
  VehicleProfilePage,
} from "@/features/fleet/VehiclesPage";

export function CentreOperationsPreviewPage() {
  const { centreId = "" } = useParams();
  return (
    <div className="bg-muted/20">
      <div className="flex flex-wrap items-center gap-2 border-b bg-card px-4 py-3">
        <Button
          variant="ghost"
          render={<Link to={`/admin/centres/${centreId}`} />}
        >
          <ArrowLeft /> Centre profile
        </Button>
        <span className="mx-1 h-5 w-px bg-border" />
        <Button
          variant="outline"
          render={<Link to={`/admin/centres/${centreId}/routes`} />}
        >
          <Route /> View routes
        </Button>
        <Button
          variant="outline"
          render={<Link to={`/admin/centres/${centreId}/vehicles`} />}
        >
          <BusFront /> View fleet
        </Button>
        <span className="ml-auto flex items-center gap-1.5 text-xs text-muted-foreground">
          <MapPinned className="size-3.5" /> Read-only operational oversight
        </span>
      </div>
      <CentreConsolePage centreId={centreId} />
    </div>
  );
}

export function CentreRoutesViewPage() {
  const { centreId = "" } = useParams();
  return (
    <RoutesPage
      centreId={centreId}
      basePath={`/admin/centres/${centreId}/routes`}
      readOnly
    />
  );
}

export function CentreRouteDetailViewPage() {
  const { centreId = "" } = useParams();
  return (
    <RouteDetailPage basePath={`/admin/centres/${centreId}/routes`} readOnly />
  );
}

export function CentreVehiclesViewPage() {
  const { centreId = "" } = useParams();
  return (
    <VehiclesPage
      centreId={centreId}
      basePath={`/admin/centres/${centreId}/vehicles`}
      readOnly
    />
  );
}

export function CentreVehicleDetailViewPage() {
  const { centreId = "" } = useParams();
  return (
    <VehicleProfilePage
      centreId={centreId}
      basePath={`/admin/centres/${centreId}/vehicles`}
      readOnly
    />
  );
}
