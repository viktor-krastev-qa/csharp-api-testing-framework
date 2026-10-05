# Разбор преди интервю

1. Обясни GET/POST/PUT/DELETE, 200/201/204/400/404/409/415 и Location.
2. Прочети contract в README и сравни с ApiHost.cs.
3. Разгледай RequestParser: JSON, типове, граници, неизвестни/дублирани полета.
4. Проследи StartApi -> HttpClient -> request -> endpoint -> store -> response -> Assert -> StopApi.
5. Обясни async/await, using var, record и Json serialization.
6. Сравни PUT замяна и PATCH частична промяна; този проект няма PATCH.
7. Разгледай как invalid PUT и conflict се проверяват за липса на mutation.
8. Добави сам нов test case и обясни риска, който покрива.

Кодът е подготвен с AI помощ. На интервю описвай това, което можеш да проследиш и промениш самостоятелно.
