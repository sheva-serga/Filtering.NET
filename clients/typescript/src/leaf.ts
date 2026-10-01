import type { FilterLeaf, FilterValue, JsonValue } from './types.js';

export interface FieldRef {
  eq(value: FilterValue): FilterLeaf;
  ne(value: FilterValue): FilterLeaf;
  gt(value: FilterValue): FilterLeaf;
  gte(value: FilterValue): FilterLeaf;
  lt(value: FilterValue): FilterLeaf;
  lte(value: FilterValue): FilterLeaf;
  contains(value: FilterValue): FilterLeaf;
  startsWith(value: FilterValue): FilterLeaf;
  endsWith(value: FilterValue): FilterLeaf;
  in(values: readonly FilterValue[]): FilterLeaf;
  isNull(): FilterLeaf;
  op(name: string, value?: FilterValue): FilterLeaf;
}

export function field(name: string): FieldRef {
  const binary = (operatorName: string, value: FilterValue): FilterLeaf => ({
    field: name,
    op: operatorName,
    value: toJsonValue(value),
  });
  const unary = (operatorName: string): FilterLeaf => ({ field: name, op: operatorName });

  return {
    eq: (value) => binary('eq', value),
    ne: (value) => binary('ne', value),
    gt: (value) => binary('gt', value),
    gte: (value) => binary('gte', value),
    lt: (value) => binary('lt', value),
    lte: (value) => binary('lte', value),
    contains: (value) => binary('contains', value),
    startsWith: (value) => binary('startsWith', value),
    endsWith: (value) => binary('endsWith', value),
    in: (values) => binary('in', values),
    isNull: () => unary('isNull'),
    op: (operatorName, value) => (value === undefined ? unary(operatorName) : binary(operatorName, value)),
  };
}

// JSON.stringify would silently turn NaN/Infinity into null and an invalid Date into null; both change the filter's meaning.
function toJsonValue(value: FilterValue): JsonValue {
  if (value instanceof Date) {
    return value.toISOString();
  }
  if (typeof value === 'number' && !Number.isFinite(value)) {
    throw new TypeError(`Filter values must be finite numbers, got ${value}.`);
  }
  if (Array.isArray(value)) {
    return (value as readonly FilterValue[]).map(toJsonValue);
  }
  return value as JsonValue;
}
