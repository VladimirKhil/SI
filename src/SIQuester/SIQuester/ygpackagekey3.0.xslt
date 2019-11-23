<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:yg="http://ur-quan1986.narod.ru/ygpackage3.0.xsd">
  <xsl:output encoding="utf-8" method="html" indent="no"/>
  <!--Документ в целом-->
  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
    <html>
      <head>
        <title>
          Вопросы SIGame
        </title>
        <meta http-equiv="Content-Type" content="text/html; charset: utf-8" />
        <style>
          .sym {
            color: red;
          }
        </style>
       <script type="text/javascript">
          <xsl:text disable-output-escaping="yes">var b = 0;

          function f1() {

          var button = document.getElementById("button1");
          var right = document.getElementsByClassName("rightans");
          var wrong = document.getElementsByClassName("wrongans");
          var c = right.length;
          var c1 = wrong.length;

          if (!b) {
          b = 1;
          button.value="Сделать ответы видимыми";

          for(var i = 0; i &lt; c; i++)
          right[i].style.color="#FDFDE0";

          for(var i = 0; i &lt; c1; i++)
          wrong[i].style.color="#FDFDE0";
          }
          else {
          b = 0;
          button.value="Сделать ответы невидимыми";
          for(var i = 0; i &lt; c; i++)
          right[i].style.color="red";

          for(var i = 0; i &lt; c1; i++)
          wrong[i].style.color="blue";
          }
          }</xsl:text>
        </script>
      </head>
      <body style="background-color: #FDFDE0; font-family: Arial">
        <b>
          <center style="font-size: 30pt">
            Вопросы SIGame
          </center>
          <center style="font-size: 25pt">
            Метасимволы в ответах
          </center>
          <br />
          <center>
            <span class="sym">{строка1} {строка2} {строка3} ...</span> - эти строки могут идти в ответе в любом порядке (например, элементы перечисления)
          </center>
          <br />
          <form id="form1">
            <input type="button" id="button1" value="Сделать ответы невидимыми" onClick="f1()" OnDblClick="f1()"/>
          </form>
          <xsl:apply-templates/>
        </b>
      </body>
    </html>
  </xsl:template>
  <!--Пакет-->
  <xsl:template match="yg:package">
    <center style="font-size: 30pt">
      ПАКЕТ <xsl:value-of select="@name"/>
    </center>
    <xsl:apply-templates/>
  </xsl:template>
  <!--Раунды-->
  <xsl:template match="yg:rounds">
    <xsl:apply-templates/>
  </xsl:template>
  <!--Раунд-->
  <xsl:template match="yg:round">
    <BR />
    <center style="font-size: 30pt">
      <xsl:value-of select="@name"/>
    </center>
    <xsl:choose>
      <xsl:when test="@type='final'">
        <center>
          (финал)
        </center>
      </xsl:when>
    </xsl:choose>
    <xsl:apply-templates/>
  </xsl:template>
  <!--Темы-->
  <xsl:template match="yg:themes">
    <xsl:apply-templates/>
  </xsl:template>
  <!--Тема-->
  <xsl:template match="yg:theme">
    <xsl:value-of select="@name"/>
    <br />
    <xsl:apply-templates/>
  </xsl:template>
  <!--Вопросы-->
  <xsl:template match="yg:questions">
    <xsl:apply-templates/>
  </xsl:template>
  <!--Информация-->
  <xsl:template match="yg:info">
    <xsl:apply-templates/>
  </xsl:template>
  <!--Вопрос-->
  <xsl:template match="yg:question">
    <xsl:choose>
      <xsl:when test="@price!='0'">
        <xsl:value-of select="@price"/>
        <br />
        <xsl:apply-templates select="yg:type" />
      </xsl:when>
    </xsl:choose>
    <xsl:apply-templates select="yg:authors" />
    <xsl:apply-templates select="yg:scenario" />
    <xsl:apply-templates select="yg:right" />
    <xsl:apply-templates select="yg:wrong" />
    <xsl:apply-templates select="yg:comments" />
    <xsl:apply-templates select="yg:sources" />
    <br />
  </xsl:template>
  <!--Тип вопроса-->
  <xsl:template match="yg:type">
    <span>
      <xsl:choose>
        <xsl:when test="@name='simple'" />
        <xsl:when test="@name='cat'">
          <xsl:attribute name="style">color:magenta</xsl:attribute>
          КОТ В МЕШКЕ<br />
        </xsl:when>
        <xsl:when test="@name='auction'">
          <xsl:attribute name="style">color:#1A961A</xsl:attribute>
          АУКЦИОН<br />
        </xsl:when>
        <xsl:when test="@name='bagcat'">
          <xsl:attribute name="style">color:magenta</xsl:attribute>
          КОТ В МЕШКЕ<br />
        </xsl:when>
        <xsl:when test="@name='sponsored'">
          <xsl:attribute name="style">color:blue</xsl:attribute>
          ВОПРОС ОТ СПОНСОРА<br />
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@name"/>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:apply-templates />
    </span>
  </xsl:template>
  <!--Параметр типа-->
  <xsl:template match="yg:param">
    <xsl:choose>
      <xsl:when test="@name='theme'">
        Тема:
      </xsl:when>
      <xsl:when test="@name='cost'">
        Стоимость:
      </xsl:when>
      <xsl:when test="@name='self'">
        Можно отдать себе:
      </xsl:when>
      <xsl:when test="@name='knows'">
        Информация о Коте узнаётся:
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:value-of select="."/>
    <br />
  </xsl:template>
  <!--Сценарий-->
  <xsl:template match="yg:scenario">
    <xsl:apply-templates/>
  </xsl:template>
  <!--Единица сценария-->
  <xsl:template match="yg:atom">
    <xsl:choose>
      <xsl:when test="@type='text'">
        <xsl:value-of select="."/>
      </xsl:when>
      <xsl:when test="@type='say'">
        <span style="font-size: 16pt"> Устно: </span>
        <span style="font-family: Courier">
          <xsl:value-of select="."/>
        </span>
      </xsl:when>
      <xsl:when test="@type='image'">
        <img>
          <xsl:attribute name="src">
            <xsl:value-of select="."/>
          </xsl:attribute>
        </img>
      </xsl:when>
      <xsl:when test="@type='voice'">
        <span style="font-size: 16pt">СЛУШАЕМ:</span>
        <br />
        <audio controls="controls">
          <source>
            <xsl:attribute name="src">
              <xsl:value-of select="."/>
            </xsl:attribute>
          </source>
        </audio>
      </xsl:when>
      <xsl:when test="@type='video'">
        <span style="font-size: 16pt">СМОТРИМ ВИДЕО:</span>
        <br />
        <video controls="controls">
          <source>
            <xsl:attribute name="src">
              <xsl:value-of select="."/>
            </xsl:attribute>
          </source>
        </video>
      </xsl:when>
    </xsl:choose>
    <xsl:if test="@time > 0">
      (<xsl:value-of select="@time"/> сек.)
    </xsl:if>
    <br />
  </xsl:template>
  <!--Правильные ответы-->
  <xsl:template match="yg:right">
    ОТВЕТ<xsl:if test="count(yg:answer) > 1">Ы</xsl:if>
    <span style="color: red" class="rightans">
      <xsl:apply-templates/>
    </span>
    <br />
  </xsl:template>
  <!--Неправильные ответы-->
  <xsl:template match="yg:wrong">
    <xsl:if test="count(yg:answer) > 0">
      <br />
      НЕ ПРИНИМАТЬ
      <span style="color: blue" class="wrongans">
        <xsl:apply-templates/>
      </span>
      <br />
    </xsl:if>
  </xsl:template>
  <!--Ответ-->
  <xsl:template match="yg:answer">
    <br />
    <xsl:value-of select="."/>
  </xsl:template>
  <!--Авторы-->
  <xsl:template match="yg:authors">
    <xsl:if test="count(yg:author) > 0">
      <span style="font-size: 16pt">
        Автор<xsl:if test="count(yg:author) > 1">ы</xsl:if>:
      </span>
      <ul>
        <xsl:apply-templates/>
      </ul>
    </xsl:if>
  </xsl:template>
  <!--Автор-->
  <xsl:template match="yg:author">
    <li>
      <xsl:value-of select="."/>
    </li>
  </xsl:template>
  <!--Источники-->
  <xsl:template match="yg:sources">
    <xsl:if test="count(yg:source) > 0">
      <span style="font-size: 16pt">
        Источник<xsl:if test="count(yg:source) > 1">и</xsl:if>:
      </span>
      <ol>
        <xsl:apply-templates/>
      </ol>
    </xsl:if>
  </xsl:template>
  <!--Источник-->
  <xsl:template match="yg:source">
    <li>
      <xsl:value-of select="."/>
    </li>
  </xsl:template>
  <!--Комментарии-->
  <xsl:template match="yg:comments">
    <xsl:if test=". != ''">
      <span style="font-size: 16pt">
        Комментарии:
      </span>
      <xsl:value-of select="."/>
      <br />
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>