export type JsonValue =
  | string
  | number
  | boolean
  | null
  | readonly JsonValue[]
  | { readonly [key: string]: JsonValue };

export interface FilterLeaf {
  readonly field: string;
  readonly op: string;
  readonly value?: JsonValue;
}

export type FilterGroup =
  | { readonly and: readonly FilterNode[] }
  | { readonly or: readonly FilterNode[] }
  | { readonly not: readonly [FilterNode] };

export type FilterNode = FilterLeaf | FilterGroup;

export type SortDir = 'asc' | 'desc';

export interface SortItem {
  readonly field: string;
  readonly dir?: SortDir;
}

export interface FilterRequest {
  readonly where?: FilterNode;
  readonly sort?: readonly SortItem[];
  readonly page?: number;
  readonly pageSize?: number;
}

export type FilterValue = JsonValue | Date | readonly FilterValue[];

export type FilterChild = FilterNode | false | null | undefined | '';
