# Настройка

Копирай ZIP съдържанието в E:\GitHub\csharp-api-testing-framework. Запази .git и LICENSE. Замени README и .gitignore.

Използваме същия .NET 9 като Project #4. Няма Python .venv. Изпълни dotnet restore и dotnet test от README. Не е нужно да стартираш API отделно: всеки test case създава собствен Kestrel на свободен локален порт.

За HTML отчет използвай py -3.13 scripts/trx_to_html.py --input TestResults/api-tests.trx --output TestResults/report.html. TRX е истинското test evidence, а HTML е негово представяне.

При успех копираме отчетите в examples/local, актуализираме Validation и настройваме локалния Git автор с точния noreply адрес преди commit.
