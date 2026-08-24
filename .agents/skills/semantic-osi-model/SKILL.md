---
name: semantic-osi-model
description: |
  Apply the Open Semantic Interchange (OSI) model to design, validate, and document
  semantic data exchange between systems. Use when: (1) designing semantic interoperability
  between heterogeneous systems, (2) modelling data exchange using OSI layers,
  (3) validating semantic compatibility of data formats, (4) documenting semantic contracts
  between services, (5) asked about open semantic interchange or OSI semantic model.
---

# Semantic OSI Model

Apply the Open Semantic Interchange (OSI) model to structure, validate, and document semantic data exchange between systems.

## Overview

The Open Semantic Interchange (OSI) model defines a layered framework for semantic interoperability — enabling systems to understand not just the syntax, but the **meaning** of exchanged data.

Official documentation: [https://open-semantic-interchange.org/](https://open-semantic-interchange.org/)

## Core Principles

1. **Layer Separation** — Each OSI layer handles a distinct concern (transport, syntax, semantics). Do not mix concerns across layers.
2. **Semantic Contracts** — Define explicit contracts that describe what data means, not just its shape.
3. **Interoperability First** — Design for heterogeneous consumers; avoid assuming shared internal models.
4. **Progressive Enrichment** — Lower layers carry raw data; higher layers add meaning and context.

## Layer Model

Apply each layer when designing or reviewing a semantic exchange:

| Layer | Concern | Artefact |
|---|---|---|
| Transport | Delivery (HTTP, AMQP, gRPC) | Protocol config |
| Syntactic | Format (JSON, XML, Protobuf) | Schema (JSON Schema, XSD) |
| Semantic | Meaning (ontology, vocabulary) | Ontology / vocabulary file |
| Pragmatic | Intent & context (why, for whom) | Semantic contract / API spec |

## Decision Tree

```
Are you designing data exchange?
├─ Define Transport layer first (protocol + reliability guarantees)
└─ Then define Syntactic layer (format + schema)
    └─ Then define Semantic layer (ontology or controlled vocabulary)
        └─ Then define Pragmatic layer (contract: intent + consumer context)

Are you validating an existing exchange?
├─ Check Transport — is delivery guaranteed?
├─ Check Syntactic — does the schema validate?
├─ Check Semantic — is the vocabulary shared or mapped?
└─ Check Pragmatic — is the consumer context preserved?
```

## Common Patterns

### Semantic Contract (Pragmatic Layer)
Define what data means in context for a given consumer:
```yaml
semantic-contract:
  producer: order-service
  consumer: billing-service
  entity: Order
  vocabulary: https://open-semantic-interchange.org/vocab/order
  intent: trigger-invoice
  version: "1.0"
```

### Vocabulary Mapping
When consumers use different terms, provide an explicit mapping:
```json
{
  "source_term": "customer_id",
  "target_term": "client_ref",
  "vocabulary": "https://open-semantic-interchange.org/vocab/crm",
  "equivalence": "exact"
}
```

## Validation Checklist

When designing or reviewing a semantic exchange, verify:
- [ ] Transport layer is defined and reliable
- [ ] Syntactic schema is machine-readable and versioned
- [ ] Semantic vocabulary is published and referenced by URI
- [ ] Semantic contract documents intent and consumer context
- [ ] Vocabulary mappings cover all cross-system term differences
- [ ] Breaking changes are versioned (never modify a published vocabulary URI)

## Official YAML Example

The following is an excerpt from the official OSI example ([`tpcds_semantic_model.yaml`](https://github.com/open-semantic-interchange/OSI/blob/main/examples/tpcds_semantic_model.yaml)) demonstrating a complete semantic model with datasets, fields, relationships, and metrics:

```yaml
# yaml-language-server: $schema=../core-spec/osi-schema.json
version: "0.1.1"

semantic_model:
  - name: tpcds_retail_model
    description: TPC-DS retail semantic model for sales and customer analytics
    ai_context:
      instructions: "Use this semantic model for retail analytics."

    datasets:
      - name: store_sales
        source: tpcds.public.store_sales
        primary_key: [ss_item_sk, ss_ticket_number]
        description: Fact table containing all store sales transactions
        ai_context:
          synonyms:
            - "sales transactions"
            - "retail sales"
        fields:
          - name: ss_ext_sales_price
            expression:
              dialects:
                - dialect: ANSI_SQL
                  expression: ss_ext_sales_price
            description: Extended sales price (quantity * price)
            ai_context:
              synonyms:
                - "total price"
                - "line total"

      - name: customer
        source: tpcds.public.customer
        primary_key: [c_customer_sk]
        description: Customer dimension with demographic information
        ai_context:
          synonyms:
            - "customers"
            - "buyers"
        fields:
          - name: customer_full_name
            expression:
              dialects:
                - dialect: ANSI_SQL
                  expression: c_first_name || ' ' || c_last_name
            description: Customer full name (computed field)
            ai_context:
              synonyms:
                - "full name"
                - "customer name"

    relationships:
      - name: store_sales_to_customer
        from: store_sales
        to: customer
        from_columns: [ss_customer_sk]
        to_columns: [c_customer_sk]
        ai_context:
          synonyms:
            - "who bought"

    metrics:
      - name: total_sales
        expression:
          dialects:
            - dialect: ANSI_SQL
              expression: SUM(store_sales.ss_ext_sales_price)
        description: Total sales revenue across all transactions
        ai_context:
          synonyms:
            - "total revenue"
            - "gross sales"

      - name: customer_lifetime_value
        expression:
          dialects:
            - dialect: ANSI_SQL
              expression: >
                SUM(store_sales.ss_ext_sales_price)
                / COUNT(DISTINCT customer.c_customer_sk)
        description: Average lifetime sales value per customer
        ai_context:
          synonyms:
            - "CLV"
            - "LTV"
```

Key structural elements to replicate:
- `version` — always specify the OSI spec version (current: `0.1.1`)
- `semantic_model[].datasets` — logical business entities with `source`, `primary_key`, `fields`
- `fields[].expression.dialects` — multi-dialect SQL expressions (ANSI_SQL, Snowflake, etc.)
- `ai_context.synonyms` — natural language aliases enabling AI grounding
- `relationships` — foreign key joins between datasets
- `metrics` — KPIs defined at model level, spanning multiple datasets

## References

- Official specification: [https://open-semantic-interchange.org/](https://open-semantic-interchange.org/)
- OSI GitHub repository: [https://github.com/open-semantic-interchange/OSI](https://github.com/open-semantic-interchange/OSI)
- Full example file: [tpcds_semantic_model.yaml](https://github.com/open-semantic-interchange/OSI/blob/main/examples/tpcds_semantic_model.yaml)
