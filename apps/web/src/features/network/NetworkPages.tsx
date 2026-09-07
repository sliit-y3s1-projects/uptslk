import { ResourcePage } from "@/features/resource/ResourcePage";
export function StopsPage() { return <ResourcePage title="Stops" description="Manage boarding points, accessibility information, and route coverage." collection="stops" primaryAction="Add stop" view="cards" />; }
export function ServiceAreasPage() { return <ResourcePage title="Service areas" description="Monitor regional coverage, service health, and depot responsibility." collection="areas" primaryAction="Add service area" view="cards" />; }
