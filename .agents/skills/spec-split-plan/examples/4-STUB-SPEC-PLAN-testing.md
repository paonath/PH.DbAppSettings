<!-- Source: source-tour-booking.md | Lines: 39–44 -->
# Stub Spec Plan: 4 — Testing

**Area**: `testing`
**Source**: `source-tour-booking.md` lines 39–44

## Prompt for spec-generator

Create a specification for the test strategy of a Tour Booking Portal. Tests required: (1) unit tests for the booking validator — seats > 0 and cancellation window > 48h; (2) integration test — POST /bookings returns HTTP 409 when the tour has no remaining seats; (3) E2E test — authenticated user browses, selects, and completes a booking; (4) acceptance test — verify cancellation is blocked when the tour starts in less than 48 hours. Define frameworks, test data setup, and pass criteria for each level.
