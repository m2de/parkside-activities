export interface Stats {
  sessionCount: number
  bookingCount: number
  totalCapacity: number
  bookedPlaces: number
  occupancyPct: number
  cancelledCount: number
  averagePartySize: number
}

interface Props {
  stats: Stats
}

export default function StatCards({ stats }: Props) {
  return (
    <div className="stat-cards">
      <div className="stat-card">
        <div className="stat-label">Sessions</div>
        <div className="stat-value">{stats.sessionCount}</div>
      </div>
      <div className="stat-card">
				<div className="stat-label">Bookings (parties)</div>
        <div className="stat-value">{stats.bookingCount}</div>
      </div>
			<div className="stat-card">
				<div className="stat-label">Places booked</div>
				<div className="stat-value">{stats.bookedPlaces}</div>
			</div>
      <div className="stat-card">
				<div className="stat-label">Capacity (places)</div>
        <div className="stat-value">{stats.totalCapacity}</div>
      </div>
      <div className="stat-card">
				<div className="stat-label">Occupancy (places)</div>
        <div className="stat-value">{stats.occupancyPct}%</div>
      </div>
      <div className="stat-card">
				<div className="stat-label">Cancelled bookings</div>
        <div className="stat-value">{stats.cancelledCount}</div>
      </div>
      <div className="stat-card">
				<div className="stat-label">Avg party size (places)</div>
        <div className="stat-value">{stats.averagePartySize}</div>
      </div>
    </div>
  )
}
