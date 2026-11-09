# UniEventsDotNetProject
A web application where university clubs and departments publish events, students register and receive a QR-code ticket, and organizers check attendees in at the door by scanning the code.

## API overview

- `POST /events` publishes an event.
- `GET /events` lists published events.
- `POST /events/{eventId}/registrations` registers a student and returns a ticket with a QR payload.
- `POST /checkins` checks a student in from a scanned ticket code/QR payload.
