# HealthCalendar

A healthcare appointment management system with separate interfaces for patients, healthcare workers, and administrators. Project is split into a backend (/api) and a frontend (/healthcalendar).

### Setup requirements
Node.js: 20.x (recommended stable LTS version) <br>
npm: 10.x or higher

### Running the project
```bash
cd healthcalendar
npm install
npm run dev
```
The project now runs at `http://localhost:5173`

### Existing login information

| Role    | E-mail            | Password |
|--------|------------------|----------|
| Patient | lars@gmail.com   | Aaaa4@   |
| Worker  | bong@gmail.com   | Aaaa4@   |
| Admin   | baifan@gmail.com | Aaaa4@   |

### Functionality
**Patients** can book events to an assigned healthcare-worker. An event can only be booked in a time period the healthcare-worker has set themselves to available. The patient can see all their booked events in a calendar view, including full CRUD operations. <br>

**Workers** can see events their patients have booked with them in the calendar view. A worker can click the 'change availability' button, to then click on individual time boxes, toggling their availability. If the worker clicks the 'repeat weekly' toggle, the availability will repeat forever weekly. Clicking on an event gives the worker more details.

**Admins**  can see a list of every healthcare-worker, as well as which patients have been assigned to them. The admin can change which patients are assigned to each worker, as well as assigning unassigned patients to a worker.