# Food reference data

`indb-foods.json` is derived from the **Indian Nutrient Databank (INDB)**, licensed CC BY:

> *Development of an Indian Food Composition Database.* Current Developments in Nutrition, 2024 (Jaacks lab, University of Edinburgh, with Anuvaad Solutions).
> https://pmc.ncbi.nlm.nih.gov/articles/PMC11277795/ · https://github.com/lindsayjaacks/Indian-Nutrient-Databank-INDB-

INDB is built mainly on ICMR-NIN's Indian Food Composition Tables (IFCT 2017).

Values are per 100 g; `unit`/`unitGrams` give the weight of one INDB serving unit (e.g. one idli).
Rows were filtered when importing:
- energy that doesn't match 4·protein + 4·carbs + 9·fat within 25% (mostly soups with inflated macros);
- more than 30 g fat per 100 g (deep-fried recipes where all frying oil was counted as eaten).

Lookups are still cross-checked against the AI estimate at runtime (see `NutritionApiService`).
