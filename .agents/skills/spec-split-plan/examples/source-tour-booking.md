# Tour Booking Portal — Analysis Document

## Overview

A web application for booking guided tours. Users browse available tours, make bookings, and receive confirmation emails.

## Data Model

- **Tour**: id, name, description, price, maxSeats, startDate, endDate
- **Booking**: id, tourId, userId, seats, status (pending/confirmed/cancelled), createdAt
- **User**: id, email, name, role (customer/admin)

Relationships: one Tour → many Bookings; one User → many Bookings.

## API Endpoints

- `GET /tours` — list tours with filters (date range, price)
- `GET /tours/{id}` — tour detail
- `POST /bookings` — create booking (requires auth)
- `GET /bookings/{id}` — booking detail (owner or admin only)
- `PATCH /bookings/{id}/cancel` — cancel booking
- `GET /admin/bookings` — all bookings (admin only)

Business rules:
- Cannot book a tour with 0 remaining seats.
- Cancellation allowed up to 48 hours before start date.

## Frontend

Angular 19 SPA:
- `/tours` — tour list page with search/filter
- `/tours/:id` — tour detail + booking form
- `/my-bookings` — user's booking history
- `/admin/bookings` — admin table (AG Grid)

## Test Plan

- Unit: booking validator (seats > 0, cancellation window)
- Integration: POST /bookings returns 409 when fully booked
- E2E: user completes full booking flow
- Acceptance: cancellation blocked < 48h before start
