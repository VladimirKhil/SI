<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:yg="http://ur-quan1986.narod.ru/ygpackage3.0.xsd">
  <xsl:output encoding="utf-8" method="html" indent="no" />
  <!--Документ в целом-->
  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes">&lt;!DOCTYPE html&gt;</xsl:text>
    <html>
      <head>
        <title>
          Вопросы SIGame
        </title>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
      </head>
      <body style="background-color: #FFFFFF; font-family: Arial">
        <b>
          <center style="font-size: 15pt">
            Метасимволы в ответах
          </center>
          <center>
            {строка1} {строка2} {строка3} ... - эти строки могут идти в ответе в любом порядке (например, элементы перечисления)
          </center>
          <br />
          <xsl:apply-templates/>
        </b>
      </body>
    </html>
  </xsl:template>
  <!--Пакет-->
  <xsl:template match="yg:package">
    <center style="font-size: 24pt">
      <xsl:value-of select="@name"/>
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
    <center style="font-size: 24pt">
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
    <span style="font-size: 12pt">
      <xsl:value-of select="@name"/>
    </span>
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
    <span style="font-size: 12pt">
      <xsl:choose>
        <xsl:when test="@price!='0'">
          <xsl:value-of select="@price"/> 
          <xsl:apply-templates select="yg:type" />
          <br />
        </xsl:when>
      </xsl:choose>
      <xsl:apply-templates select="yg:authors" />
      <xsl:apply-templates select="yg:scenario" />
      <xsl:apply-templates select="yg:right" />
      <xsl:apply-templates select="yg:wrong" />
      <xsl:apply-templates select="yg:comments" />
      <xsl:apply-templates select="yg:sources" />
      <br />
    </span>
  </xsl:template>
  <!--Тип вопроса-->
  <xsl:template match="yg:type">
    <span>
      <xsl:choose>
        <xsl:when test="@name='simple'" />
        <xsl:when test="@name='cat'">
          КОТ В МЕШКЕ 
        </xsl:when>
        <xsl:when test="@name='auction'">
          АУКЦИОН 
        </xsl:when>
        <xsl:when test="@name='bagcat'">
          КОТ В МЕШКЕ
        </xsl:when>
        <xsl:when test="@name='sponsored'">
          ВОПРОС ОТ СПОНСОРА
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
        (изображение)
      </xsl:when>
      <xsl:when test="@type='voice'">
        (звук)
      </xsl:when>
      <xsl:when test="@type='video'">
        (видео)
      </xsl:when>
    </xsl:choose>
    <xsl:if test="@time > 0">
      (<xsl:value-of select="@time"/> сек.)
    </xsl:if>
    <br />
  </xsl:template>
  <!--Правильные ответы-->
  <xsl:template match="yg:right">
    ОТВЕТ<xsl:if test="count(yg:answer) > 1">Ы</xsl:if>: 
      <xsl:apply-templates/>
    <br />
  </xsl:template>
  <!--Неправильные ответы-->
  <xsl:template match="yg:wrong">
    <xsl:if test="count(yg:answer) > 0">
      НЕ ПРИНИМАТЬ:
      <xsl:apply-templates/>
    <br />
    </xsl:if>
  </xsl:template>
  <!--Ответ-->
  <xsl:template match="yg:answer">
    <xsl:value-of select="."/>
    <xsl:choose>
      <xsl:when test="position() = last()">.</xsl:when>
      <xsl:otherwise>,</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--Авторы-->
  <xsl:template match="yg:authors">
    <xsl:if test="count(yg:author) > 0">
      <span style="font-size: 16pt">
        Автор<xsl:if test="count(yg:author) > 1">ы</xsl:if>:
      </span>
      <xsl:apply-templates/>
      <br />
    </xsl:if>
  </xsl:template>
  <!--Автор-->
  <xsl:template match="yg:author">
    <i>
      <xsl:value-of select="."/>
    </i>
    <xsl:choose>
      <xsl:when test="position() = last()">.</xsl:when>
      <xsl:otherwise>,</xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  <!--Источники-->
  <xsl:template match="yg:sources">
    <xsl:if test="count(yg:source) > 0">
      <span style="font-size: 16pt">
        Источник<xsl:if test="count(yg:source) > 1">и</xsl:if>:
      </span>
      <xsl:apply-templates/>
      <br />
    </xsl:if>
  </xsl:template>
  <!--Источник-->
  <xsl:template match="yg:source">
    <i>
      <xsl:value-of select="."/>
    </i>
    <xsl:choose>
      <xsl:when test="position() = last()">.</xsl:when>
      <xsl:otherwise>,</xsl:otherwise>
    </xsl:choose>
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