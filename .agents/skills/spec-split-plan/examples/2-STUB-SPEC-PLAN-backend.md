<!-- Source: source-tour-booking.md | Lines: 14–27 -->
# Stub Spec Plan: 2 — Backend

**Area**: `backend`
**Source**: `source-tour-booking.md` lines 14–27

## Prompt for spec-generator

Create a specification for the REST API of a Tour Booking Portal. Endpoints: GET /tours (list with date/price filters), GET /tours/{id} (detail), POST /bookings (authenticated, creates booking), GET /bookings/{id} (owner or admin), PATCH /bookings/{id}/cancel (cancel booking), GET /admin/bookings (admin only). Business rules: a booking must be rejected (409 Conflict) if no seats remain; cancellation is only allowed more than 48 hours before the tour start date. Authentication is required for all booking endpoints.
