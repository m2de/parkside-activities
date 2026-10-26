export interface ScheduleSession {
  id: number
  activity: string
  start_time: string
  duration_minutes: number
  capacity: number
	booked_places: number
}

interface Props {
  sessions: ScheduleSession[]
}

export default function ScheduleTable({ sessions }: Props) {
  return (
    <div className="panel">
      <table className="data-table">
        <thead>
          <tr>
            <th>Time</th>
            <th>Activity</th>
            <th>Duration</th>
						<th>Capacity (places)</th>
						<th>Places booked</th>
          </tr>
        </thead>
        <tbody>
          {sessions.map((session) => (
            <tr key={session.id}>
              <td>{session.start_time}</td>
              <td>{session.activity}</td>
              <td>{session.duration_minutes} min</td>
              <td>{session.capacity}</td>
							<td>{session.booked_places}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
