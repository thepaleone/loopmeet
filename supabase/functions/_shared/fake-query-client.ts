import type { SupabaseClient } from "npm:@supabase/supabase-js@2";

export interface Query {
  table: string;
  columns: string;
  filters: [string, string][];
}

export interface QueryResult {
  data: unknown;
  error: { message: string } | null;
}

// Answers every `from(table).select(columns).eq(column, value)` query, awaited directly or
// through maybeSingle(), with `result`, and records what was asked.
export const fakeClient = (result: (query: Query) => QueryResult) => {
  const queries: Query[] = [];
  const client = {
    from: (table: string) => ({
      select: (columns: string) => {
        const query: Query = { table, columns, filters: [] };
        queries.push(query);
        const run = () => Promise.resolve(result(query));
        return {
          eq: (column: string, value: string) => {
            query.filters.push([column, value]);
            return {
              maybeSingle: run,
              then: <T>(onfulfilled: (value: QueryResult) => T) => run().then(onfulfilled),
            };
          },
        };
      },
    }),
  };
  return { client: client as unknown as SupabaseClient, queries };
};
