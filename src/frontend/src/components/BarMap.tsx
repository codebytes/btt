import { CircleMarker, MapContainer, Popup, TileLayer } from 'react-leaflet';
import type { BarLocation } from '../api';

type BarMapProps = {
  bar: BarLocation;
  compact?: boolean;
};

export function BarMap({ bar, compact = false }: BarMapProps) {
  const position: [number, number] = [bar.lat, bar.lng];

  return (
    <div className={compact ? 'map-card map-card--compact' : 'map-card'}>
      <MapContainer center={position} zoom={15} scrollWheelZoom={false} className="leaflet-map">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <CircleMarker center={position} radius={10} pathOptions={{ color: '#b84418', fillColor: '#e66a2c', fillOpacity: 0.9 }}>
          <Popup>{bar.name}</Popup>
        </CircleMarker>
      </MapContainer>
    </div>
  );
}
