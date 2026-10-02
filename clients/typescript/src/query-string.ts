import { request, type RequestParts } from './request.js';
import type { FilterNode, FilterRequest, SortItem } from './types.js';

type QueryParameter = 'where' | 'sort' | 'page' | 'pageSize';

export class FilterQueryStringError extends Error {
  readonly parameter: QueryParameter;

  constructor(parameter: QueryParameter, message: string) {
    super(message);
    this.name = 'FilterQueryStringError';
    this.parameter = parameter;
  }
}

export function toQueryString(filterRequest: FilterRequest): string {
  const parameters = new URLSearchParams();
  if (filterRequest.where !== undefined) {
    parameters.append('where', JSON.stringify(filterRequest.where));
  }
  for (const item of filterRequest.sort ?? []) {
    parameters.append('sort', item.dir === undefined ? item.field : `${item.field}:${item.dir}`);
  }
  if (filterRequest.page !== undefined) {
    parameters.append('page', String(filterRequest.page));
  }
  if (filterRequest.pageSize !== undefined) {
    parameters.append('pageSize', String(filterRequest.pageSize));
  }
  return parameters.toString();
}

// URLSearchParams already drops one leading "?" from a string.
export function fromQueryString(query: string | URLSearchParams): FilterRequest {
  const parameters = typeof query === 'string' ? new URLSearchParams(query) : query;
  const values = collectOwnedValues(parameters);
  const parts: RequestParts = {};

  const whereText = readSingle(values, 'where');
  if (whereText !== undefined) {
    parts.where = parseWhere(whereText);
  }
  const sortTexts = values.sort;
  if (sortTexts.length > 0) {
    parts.sort = sortTexts.map(parseSortItem);
  }
  const pageText = readSingle(values, 'page');
  if (pageText !== undefined) {
    parts.page = parseInteger('page', pageText);
  }
  const pageSizeText = readSingle(values, 'pageSize');
  if (pageSizeText !== undefined) {
    parts.pageSize = parseInteger('pageSize', pageSizeText);
  }

  return request(parts);
}

type OwnedValues = Record<QueryParameter, string[]>;

// ASP.NET matches query keys case-insensitively, so `Page=2` must bind the same way here.
function collectOwnedValues(parameters: URLSearchParams): OwnedValues {
  const owned: OwnedValues = { where: [], sort: [], page: [], pageSize: [] };
  const parametersByLowerCaseName = new Map<string, QueryParameter>(
    (Object.keys(owned) as QueryParameter[]).map((parameter) => [parameter.toLowerCase(), parameter]),
  );
  for (const [name, value] of parameters) {
    const parameter = parametersByLowerCaseName.get(name.toLowerCase());
    if (parameter !== undefined) {
      owned[parameter].push(value);
    }
  }
  return owned;
}

// The server cannot bind two values into one scalar either, so taking the first would hide a real mismatch.
function readSingle(ownedValues: OwnedValues, parameter: Exclude<QueryParameter, 'sort'>): string | undefined {
  const values = ownedValues[parameter];
  if (values.length > 1) {
    throw new FilterQueryStringError(parameter, `'${parameter}' must appear at most once, got ${values.length} values.`);
  }
  return values[0];
}

function parseWhere(text: string): FilterNode {
  let parsed: unknown;
  try {
    parsed = JSON.parse(text);
  } catch {
    throw new FilterQueryStringError('where', "'where' is not valid JSON.");
  }
  if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
    throw new FilterQueryStringError('where', "'where' must be a JSON object.");
  }
  return parsed as FilterNode;
}

function parseSortItem(text: string): SortItem {
  const separatorIndex = text.lastIndexOf(':');
  const fieldName = separatorIndex < 0 ? text : text.slice(0, separatorIndex);
  if (fieldName.trim() === '') {
    throw new FilterQueryStringError('sort', `Sort item '${text}' has no field name.`);
  }
  if (separatorIndex < 0) {
    return { field: fieldName };
  }
  const direction = text.slice(separatorIndex + 1).toLowerCase();
  if (direction !== 'asc' && direction !== 'desc') {
    throw new FilterQueryStringError('sort', `Sort item '${text}' must end with ':asc' or ':desc'.`);
  }
  return { field: fieldName, dir: direction };
}

// Mirrors int.TryParse with NumberStyles.Integer: ASCII whitespace around an optional sign and digits.
const int32Pattern = /^[\t\n\v\f\r ]*([+-]?\d+)[\t\n\v\f\r ]*$/;

function parseInteger(parameter: 'page' | 'pageSize', text: string): number {
  const match = int32Pattern.exec(text);
  const value = match === null ? Number.NaN : Number(match[1]);
  if (!Number.isInteger(value) || value < -2147483648 || value > 2147483647) {
    throw new FilterQueryStringError(parameter, `'${parameter}' must be a 32-bit integer, got '${text}'.`);
  }
  return value;
}
