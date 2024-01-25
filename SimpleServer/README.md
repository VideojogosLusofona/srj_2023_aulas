# Servidor HTTP simples

Isto é só um servidor de HTTP muito rudimentar, para testar sockets _listening_ usando um _browser_.
Implementa:

- _Pool_ de _threads_, para tratar os pedidos, com _thread safety_
- Processamento do endereço básico. Pode tratar /test1, /test2 e /exit como caminhos.
- Permite múltiplos pedidos até que http://localhost/exit seja usado no _browser_, o que fecha o servidor

## Uso

Basta compilar e correr este projecto. Depois é só abrir um browser e colocar o endereço:
http://localhost/\<caminho\>

\<caminho\> pode ser **test1**, **test2** ou **exit**.

**test1** e **test2** retornam mensagems diferentes, com código "200 OK"

**exit** instruí o servidor para se desligar

Qualquer outro endereço leva a um erro "404 Not Found"

## Licenças

Todo o código neste repositório é disponibilizado através da licença [GPLv3].
Os textos e restantes ficheiros são disponibilizados através da licença
[CC BY-NC-SA 4.0].

## Metadados

* Autor: [Diogo de Andrade]
* Curso:  [Licenciatura em Videojogos][licvideo]
* Instituição: [Universidade Lusófona de Humanidades e Tecnologias][ULHT]

[GPLv3]:https://www.gnu.org/licenses/gpl-3.0.en.html
[CC BY-NC-SA 4.0]:https://creativecommons.org/licenses/by-nc-sa/4.0/
[licvideo]:https://www.ulusofona.pt/licenciatura/videojogos
[Diogo de Andrade]:https://github.com/DiogoDeAndrade
[ULHT]:https://www.ulusofona.pt/
