<!-- Source: source-tour-booking.md | Lines: 6–12 -->
# Stub Spec Plan: 1 — Database

**Area**: `database`
**Source**: `source-tour-booking.md` lines 6–12

## Prompt for spec-generator

Create a specification for the database schema of a Tour Booking Portal. The schema includes three entities: Tour (id, name, description, price, maxSeats, startDate, endDate), Booking (id, tourId, userId, seats, status, createdAt), and User (id, email, name, role). Relationships: one Tour has many Bookings; one User has many Bookings. Status values for Booking are: pending, confirmed, cancelled. Roles for User are: customer, admin. Define primary keys, foreign keys, and appropriate indexes.
