
function substr_yymmdd(s) {
    return (s.length>8) ? s.substring(0, 8) : s;
}

function convert_str2sec(s) {
    var sa = s.split(".");
    if (sa.length < 1) {
        console.log('invalid format');
        return -1; // invalid format
    }
    var ta = sa[0].split(":");
    if (ta.length !=3) {
        console.log('invalid format');
        return -1; // invalid format
    }
    var sec = Number(ta[0])*60*60 + Number(ta[1])*60 + Number(ta[2]);
    //console.log('sec=' + sec);
    return sec;
}

function execSearch(content_id, lang) {
	var keyword = $("#q").val();
	var lang = $("#lang").val();
    execCustomParamsSearch(content_id, lang, keyword);
}

//function execSearch(content_id, lang)
function execCustomParamsSearch(content_id, lang, keyword)
{
    // assigned given lang value to #lang
    $("#lang").val(lang);
    $("#q").val(keyword);

	var searchAPI = "/api/azuresearch-caption-api.php?content_id=" + content_id + "&lang=" + lang;
    if (keyword != '' ) {
	    searchAPI = searchAPI + "&search=" + encodeURIComponent(keyword);
    }

    $.ajax({
        url: searchAPI,
        type: "GET",
        success: function (data) {
			$( "#colcontainer2" ).html('');
			$( "#colcontainer2" ).append('<table>');
            var caption_counter = 0;
			for (var item in data.value)
			{
				var caption_id = data.value[item].id;
                var caption = data.value[item].caption;
                if ( '@search.highlights' in data.value[item] ){
				    caption = data.value[item]['@search.highlights'].caption;
                }
				var begin_sec = convert_str2sec(data.value[item].begin_s);
				var begin_s = data.value[item].begin_s;
                begin_s = substr_yymmdd(begin_s);
				var end_s = data.value[item].end_s;
                end_s = substr_yymmdd(end_s);
                var bgcolor_class = ( caption_counter % 2 ==0 ) ? "bgcolor-odd" : "bgcolor-even";
		    	$( "#colcontainer2" ).append(
                        "<tr id=\"" + caption_id + "\">\n");
                $( "#colcontainer2" ).append(
                        "<td class=\"timecol " + bgcolor_class + "\">[<a href=\"#\" id=\"dummy\" onclick=\"restart(" + begin_sec + ");\">"
                         + begin_s + " - " +  end_s + "</a>]</td>\n");
                $( "#colcontainer2" ).append(
                        "<td class=\"clickme " + bgcolor_class + "\" id=\"" + caption_id + "\">" + caption + "</td>\n");
                $( "#colcontainer2" ).append("</tr>\n");
                caption_counter++;
			}
            $( "#colcontainer2" ).append('</table>');
        }
    });

}
