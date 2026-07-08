var env = window.FEEGOW_ENVS.FC_APP_ENV;
var domain = window.FEEGOW_ENVS.FC_API_URL;
var domainApiRest = window.FEEGOW_ENVS.FC_REST_API_URL;
var odontoEndpointURL = window.FEEGOW_ENVS.FC_ODONTOGRAMA_URL;
var feegowFrontendURL = window.FEEGOW_ENVS.FC_FRONTEND_URL;
var labServiceURL = window.FEEGOW_ENVS.FC_LABS_URL;
var AutOnServiceURL = window.FEEGOW_ENVS.FC_AUTORIZADOR_ONLINE_URL;
var IAAssistantURL = window.FEEGOW_ENVS.FC_IA_ASSISTANT_URL;
var IAAssistantURLV2 = window.FEEGOW_ENVS.FC_IA_ASSISTANT_URL_V2;
var cacheDate = 0;
var api = "./api/";
var domainFinancial = window.FEEGOW_ENVS.FC_FINANCIAL2_URL;

window.fgwGetStorageItem = window.fgwGetStorageItem || function(key, fallback = '') {
    const value = sessionStorage.getItem(key) ?? localStorage.getItem(key);
    return value || fallback;
};

const initComponents = ({
        apiUrl
    }) => {
        if(apiUrl){
            window.domain = apiUrl;
        }
};

var modalTimeout = 1000;

var modal = "<div id=\"modal-components\" class=\"modal fade\" tabindex=\"-1\">" +
    "    <div \[MODAL-WIDTH]\ class=\"modal-dialog [MODAL-SIZE]\"> " +
    "       <form id='form-components' method='post'>" +
    "           <div class=\"modal-content fgw-modal-content\" id=\"modal\">" +
    "           " +
    "           </div>" +
    "       </form>" +
    "    </div>" +
    "</div>";



function getModal(loading, modalSize, modalWidth,force) {
    var modalComponents = "#modal-components";
    var $modalComponents = $(modalComponents);

    if ($modalComponents.length === 0) {

        if (!modalSize) {
            modalSize = "";
        } else if (modalSize === "lg") {
            modalSize = "modal-lg";
        } else if (modalSize === "sm") {
            modalSize = "modal-sm";
        } else if (modalSize === "md") {
            modalSize = "modal-md";
        }

        if (!modalWidth) {
            modalWidth = "";
        } else {
            modalWidth = "style='width:" + modalWidth + "'"
        }

        const isDevice = navigator.userAgent.toLowerCase().includes('android') || navigator.userAgent.toLowerCase().includes('iphone');
        if (isDevice) {
            modalSize = ""
            modalWidth = 'style="width:93.6vw!important"'
        }

        modal = modal.replace("[MODAL-SIZE]", modalSize);
        modal = modal.replace("[MODAL-WIDTH]", modalWidth);

        $("body").append(modal);

        $modalComponents = $(modalComponents);
    }

    if (loading) {
        setModalContent("    <div style=\"text-align: center; margin-bottom: 100px\" class=\"row\">\n" +
            "        <div class=\"col-md-12\">\n" +
            "            <img style=\"margin-top: 60px; width: 80px; height: 80px\" src=\"https://core.feegow.com/img/feegow-loading.gif?v=2.0\">\n" +
            "        </div>\n" +
            "    </div>");
    }

    return $modalComponents;
}

function getFormData($form){
    var unindexed_array = $form.serializeArray();
    var indexed_array = {};

    $.map(unindexed_array, function(n, i){

        if(indexed_array[n['name']]){

            if(typeof indexed_array[n['name']] !== "object"){
                indexed_array[n['name']] = [indexed_array[n['name']]];
            }

            indexed_array[n['name']].push(n['value']);
            return;
        }
        indexed_array[n['name']] = n['value'];
    });

    return indexed_array;
}

function setModalContent(body, title, closeBtn, saveBtn, params) {
    var $modalComponents = $("#modal-components");
    var content = "";

    if (title) {
        content += "<div class=\"modal-header\">\n" +
            "        <button type=\"button\" class=\"close\" data-dismiss=\"modal\">&times;</button>\n" +
            "        <h4 class=\"modal-title\">" + title + "</h4>\n" +
            "      </div>";
    }

    if (body) {
        content += "<div class=\"modal-body\">\n" +
            "        " + body +
            "      </div>";
    }

    if (closeBtn) {
        content += "<div class=\"modal-footer\">\n" +
            "        <button type=\"button\" class=\"btn btn-default\" data-dismiss=\"modal\">Fechar</button>\n";

        if (saveBtn) {
            if (saveBtn === true) {
                saveBtn = "Salvar";
            }
            var onclickEvent = "";

            if(typeof saveBtn === "function"){
                var paramsEncoded = JSON.stringify(params);
                onclickEvent = `type='button' onclick='(${saveBtn})(${paramsEncoded})'`;
                saveBtn = "Salvar";
            }


            content += "<button "+onclickEvent+" class=\"btn btn-primary components-modal-submit-btn\" ><i class='fa fa-save'/> " + saveBtn + "</button>\n";
        }

        content += "      </div>";
    }

    $modalComponents.find(".fgw-modal-content").html(content);


    return $modalComponents;
}

function getUrl(url, data, callback,ms = null) {
    if (!data) {
        data = {};
    }

    if (url.charAt(0) === "/") {
        url = url.substr(1);
    }
    
    var d = "";
	
    if (url.indexOf(".asp") === -1) {
        d = domain;
	    
        if (ms)
        {
	    d = getMicroserviceDomain(ms)
        }

        if (d.charAt(d.length - 1) !== "/") {
            d = d + "/";
        }
    }
    
    url = d + url;

	const token= fgwGetStorageItem("tk");
	$.ajaxSetup({
        headers: { 'x-access-token': token }
    });

	$.ajax({
		type: 'GET',
		url: url,
		data: data,
		//OR
		//beforeSend: function(xhr) {
		//  xhr.setRequestHeader("My-First-Header", "first value");
		//  xhr.setRequestHeader("My-Second-Header", "second value");
		//}
        error: function(data) {
            if (callback) {
                callback("ERROR");
            }
        }
	}).done(function(data) {
		if (callback) {
            callback(data);
        }
    });
}

/**
 * Busca dados de endereço pelo CEP usando API configurável via variável de ambiente
 * @param {string} cep - CEP a ser consultado (com ou sem formatação)
 * @returns {Promise<Object>} Promise com objeto contendo: logradouro, bairro, cidade, uf, cep, pais
 * 
 * A URL da API é configurada via window.FEEGOW_ENVS.FC_CEP_API_URL
 * Use {cep} como placeholder na URL. Exemplos:
 * - ViaCEP: https://viacep.com.br/ws/{cep}/json/
 * - OpenCEP: https://opencep.com/v1/{cep}
 * - BrasilAPI: https://brasilapi.com.br/api/cep/v1/{cep}
 */
function buscaCep(cep) {
    return new Promise(function(resolve, reject) {
        // Remove caracteres não numéricos
        cep = (cep || '').replace(/\D/g, '');

        // Verifica se o CEP está vazio
        if (!cep) {
            reject('CEP não informado');
            return;
        }

        // Valida o formato do CEP (8 dígitos)
        if (!/^[0-9]{8}$/.test(cep)) {
            reject('Formato de CEP inválido');
            return;
        }

        // Obtém a URL da API via variável de ambiente, com fallback para ViaCEP
        var apiUrlTemplate = (window.FEEGOW_ENVS && window.FEEGOW_ENVS.FC_CEP_API_URL) 
            ? window.FEEGOW_ENVS.FC_CEP_API_URL 
            : 'https://viacep.com.br/ws/{cep}/json/';
        
        var apiUrl = apiUrlTemplate.replace('{cep}', cep);

        fetch(apiUrl, {
            method: 'GET'
        })
        .then(function(response) {
            if (!response.ok) {
                throw new Error('Erro na requisição: ' + response.status);
            }
            return response.json();
        })
        .then(function(data) {
            // Verifica erro (ViaCEP retorna {erro: true})
            if (data.erro) {
                reject('CEP não encontrado');
                return;
            }
            // Normaliza o retorno para o formato esperado pela aplicação
            // Compatível com ViaCEP, OpenCEP e BrasilAPI (todos usam campos similares)
            resolve({
                logradouro: data.logradouro || data.street || '',
                bairro: data.bairro || data.neighborhood || '',
                cidade: data.localidade || data.city || '',
                uf: data.uf || data.state || '',
                cep: data.cep || '',
                pais: 1 // Brasil
            });
        })
        .catch(function(error) {
            reject(error.message || 'Erro ao buscar CEP');
        });
    });
}

function getModalTermoDeUso(id){
    $("#termos-uso-barra").addClass('hidden');
    $('.navbar').css('margin-top', 0);


    //fazer verificação se já existe esse modal com esse id
    if($("#modal-termo-de-uso-"+id).length === 0){
        var modalTermoDeUso = `<div class="modal fade bd-example-modal-lg" id="modal-termo-de-uso-`+id+`" tabindex="-1" role="dialog" aria-labelledby="modaltermodeuso" aria-hidden="true">
                        <div class="modal-dialog modal-lg" role="document">
                            <div class="modal-content">
                            <div class="text-center" id="modal-body-termo-de-uso-`+id+`">
                            <img style=\"margin: 60px; width: 80px; height: 80px\" src=\"https://core.feegow.com/img/feegow-loading.gif?v=2.0\">\n
                            </div>
                            </div>
                        </div>
                        </div>`;

        $('body').append(modalTermoDeUso);
    }
    // atribuir {backdrop: 'static', keyboard: false} ao modal
    $("#modal-termo-de-uso-"+id).modal({backdrop: 'static', keyboard: false});

    $("#modal-termo-de-uso-"+id).modal("show");
    // dar um timeout pra certificar que deu tempo de carregar o tk
    setTimeout(function(){
        getUrl("termos/termo-show/"+id,{}, function(data) {
            $("#modal-body-termo-de-uso-"+id).html(data);
        },"");
    }, 500);

}


async function Select2Cid11(inputId) {
    const token = fgwGetStorageItem("tk");
    $.ajaxSetup({
        headers: { 'x-access-token': token }
    });

    
    const inputElement = $(`#${inputId}`);
    let currentRequest = null; 

    
    inputElement.select2({
        placeholder: "Digite para buscar...",
        minimumInputLength: 1,
        ajax: {
            url:  domain+"cid-11/search", 
            dataType: "json",
            delay: 250,
            transport: function (params, success, failure) {

                if (currentRequest) {
                    currentRequest.abort();
                }

                currentRequest = $.ajax(params)
                    .done(success)
                    .fail(failure);

                return currentRequest;
            },
            data: function (params) {
                return {
                    search: params.term
                };
            },
            processResults: function (data) {
                if (!data ||  data.length === 0) {
                    return {
                        results: [] 
                    };
                }

                return {
                    results: data.map(item => ({
                        id: encodeURIComponent(`{ "id":"${item.id}", "title":"${item.title}", "theCode":"${item.theCode}" }`),
                        text: `${item.theCode} : ${item.title}`
                    }))
                };
            },
            cache: true
        }
    });
}


function postUrl(url, data, callback,ms = null) {
    if (!data) {
        data = {};
    }

    var d = "";
	
    if (url.indexOf(".asp") === -1) {
        d = domain;
	    
        if (ms)
        {
	    d = getMicroserviceDomain(ms)
        }
    }
    url = d + url

    const token= fgwGetStorageItem("tk");
    $.ajax({
        type: 'POST',
        url: url,
        data: data,
        headers: {
            "x-access-token":token
        }
        //OR
        //beforeSend: function(xhr) {
        //  xhr.setRequestHeader("My-First-Header", "first value");
        //  xhr.setRequestHeader("My-Second-Header", "second value");
        //}
    }).done(function(data) {
        if (callback) {
            callback(data);
        }}).fail(function(xhr, textStatus, error) {
            //Ajax request failed.
            var mensagem  = error;
            var data = {success:false, message:mensagem};
            callback(data);
      });
}

function openModal(data, title, closeBtn, saveBtn, modalSize) {
    if (!modalSize) {
        modalSize = "lg";
    }

    var $modal = getModal(true, modalSize);
    $modal.modal("show");
    setModalContent(data, title, closeBtn, saveBtn);
}

function getMicroserviceDomain(ms){
	if(ms === 'integracaolaboratorial')
	{
	    return labServiceURL;
	}
    if(ms === 'autorizadoronline'){
        return AutOnServiceURL;
	}
    if(ms === 'odontograma')
	{
	    return `${odontoEndpointURL}/`;
	}
    if(ms === 'autorizadoronline'){
        return AutOnServiceURL;
    }

	return false;
}

function openComponentsModal(url, params, title, closeBtn, saveBtn, modalSize, modalWidth) {
    if (!modalSize) {
        modalSize = "lg";
    }
    var d = "";

    var $modal = getModal(true, modalSize, modalWidth);
    $modal.modal("show");

    if (url.indexOf(".asp") === -1) {
	d = domain;

        if (typeof(params) === 'object' && params.microservico)
        {
	    d = getMicroserviceDomain(params.microservico)
        }
    }
    url = d + url;

	const token= fgwGetStorageItem("tk");

	$.ajax({
		type: 'GET',
		url: url,
		data: params,
		headers: {
			"x-access-token":token
		}
		//OR
		//beforeSend: function(xhr) {
		//  xhr.setRequestHeader("My-First-Header", "first value");
		//  xhr.setRequestHeader("My-Second-Header", "second value");
		//}
	}).done(function(data) {
        var $modal = setModalContent(data, title, closeBtn, saveBtn, params);

        setTimeout(function () {
            setListeners($modal)
        }, modalTimeout);
	}).fail(function(xhr, textStatus, error) {
        showMessageDialog("Ocorreu um erro. Tente novamente mais tarde.", "error");
        closeComponentsModal();
    });
}

function openComponentsModalPost(url, params, title, closeBtn, saveBtn, modalSize, modalWidth) {
    if (!modalSize) {
        modalSize = "lg";
    }

    var $modal = getModal(true, modalSize, modalWidth);
    $modal.modal("show");

    if (url.indexOf(".asp") === -1) {
        url = domain + url;
    }

    const token= fgwGetStorageItem("tk");

    $.ajax({
        type: 'POST',
        url: url,
        data: params,
        headers: {
            "x-access-token":token
        }
    }).done(function(data) {
        var $modal = setModalContent(data, title, closeBtn, saveBtn, params);

        setTimeout(function () {
            setListeners($modal)
        }, modalTimeout);
    }).fail(function(xhr, textStatus, error) {
        showMessageDialog("Ocorreu um erro. Tente novamente mais tarde.", "error");
        closeComponentsModal();
    });
}

function setListeners($modal) {
    $(".components-modal-submit-btn", $modal).click(function () {
        var $btn = $(this);

        setTimeout(function () {
            $btn.attr("disabled", true);
            $btn.find("i").removeClass();
            $btn.find("i").addClass("fa fa-circle-o-notch fa-spin");
        }, 100);
    });

    $modal.on('hidden.bs.modal', function () {
        $(this).remove()
    });

    if (typeof initComponents === "function") {
        initComponents($modal);
    }
}

function getComponentUrl(url){
	const tk = fgwGetStorageItem("tk");
	return domain + url +	"?tk="+tk
}

function get$ComponentsForm(action) {
    var $form = $("#form-components");

    if (action) {
        $form.attr("action", action);
    }

    return $form;
}

function closeComponentsModal() {
    var $modal = getModal(false);

    $modal.modal('hide');
}

const params = new Proxy(new URLSearchParams(window.location.search), {
    get: (searchParams, prop) => searchParams.get(prop),
});


const fevent = (description, moduleName, params = {}, criticity = 1) => {
    try{
        if(typeof mixpanel !== 'undefined'){
            mixpanel.track(description, {...{
                'module': moduleName,
                'page': params.P,
                'pid': params.I
            }, params});
        }
        if(typeof ga !== 'undefined'){
            ga('send', 'event', 'UserError', moduleName, description);
        }
    }catch(e){
        // console.error("Erro ao registrar evento GA:"+e)
    }
}

function showMessageDialog(message, messageType, title, delay=3000) {
    if (!messageType) {
        messageType = "danger";
    }

    if (!delay) {
        delay = 3000;
    }

    var $modal = $("#form-components");

    if ($modal) {
        setTimeout(function () {
            $(".components-modal-submit-btn", $modal).attr("disabled", false);
        }, 200);
    }

    if (!title) {
        if (messageType === "danger") {
            title = "Ocorreu um erro!"
        } else if (messageType === "success") {
            title = "Sucesso!"
        } else if (messageType === "warning") {
            title = "Atenção!"
        }
    }

    new PNotify({
        title: title,
        text: message,
        type: messageType,
        delay: delay
    });
}

/**
 * Hash function to generate a CRC32 checksum from a string of parameters
 * @param str
 * @returns {string}
 */
function hashParams(str) {
    const table = Array(256).fill().map((_, n) => {
        let c = n;
        for (let k = 0; k < 8; k++) c = (c & 1) ? (0xEDB88320 ^ (c >>> 1)) : (c >>> 1);
        return c >>> 0;
    });
    let crc = -1;
    for (let i = 0; i < str.length; i++) {
        crc = (crc >>> 8) ^ table[(crc ^ str.charCodeAt(i)) & 0xFF];
    }
    return ((crc ^ -1) >>> 0).toString(16);
}

/**
 * Authenticate function to get a token from the server and set it in sessionStorage and ajax headers
 * If token already exists in sessionStorage and params hash are the same, it will use the existing token,
 * otherwise it will make a new request to get a new token
 * @param u
 * @param l
 * @param cupom
 * @param franquia
 * @param UnidadeID
 * @param il
 */
function authenticate(u, l = false, cupom="", franquia="", UnidadeID= 0, il= 0) {
    // hash all function params as string
    const tkParams = hashParams(`${u}${l}${cupom}${franquia}${UnidadeID}${il}`);

    // check if token is already in sessionStorage and if the params are the same
    if(sessionStorage.getItem("tk") && sessionStorage.getItem("htk") === tkParams) {
        $.ajaxSetup({
            headers: { 'x-access-token': sessionStorage.getItem("tk") }
        });
        return;
    }

    getUrl("auth", {l: l, _u: u, _p: cupom, _f: franquia, UnidadeID, il}, function(data) {
        if(data.success==true){
            const token = data.t;
            $.post("confAut.asp", data);

            sessionStorage.setItem("tk", token);
            sessionStorage.setItem("htk", tkParams);

            if (localStorage.getItem("tk")) {
                localStorage.removeItem("tk"); // remove old tk from localStorage if exists
            }

            $.ajaxSetup({
                headers: { 'x-access-token': token }
            });

            // send a message to feegow app if it's running on a webview with user token
            window.ReactNativeWebView?.postMessage(
                JSON.stringify({
                    type: "loginSuccess",
                    payload: {
                        token,
                        licenseId: l,
                    },
                })
            );
        } else {
            // send a message to feegow app if it's running on a webview to invalidate user token
            window.ReactNativeWebView?.postMessage(
                JSON.stringify({
                    type: "loginFailed",
                    payload: {
                        licenseId: l,
                    },
                })
            );
        }
    });
}

function authenticateSession(token, l) {
    // If valid token is passed, set it in the storage and jquery ajax headers
    if (token && token.trim()) {
        console.debug('Re-Authenticated with session token:', token);
        // send a message to feegow app if token is not in the storage and it's running on a webview
        if(!fgwGetStorageItem("tk")) {
            window.ReactNativeWebView?.postMessage(
                JSON.stringify({
                    type: "loginSuccess",
                    payload: {
                        token,
                        licenseId: l,
                    },
                })
            );
        }

        sessionStorage.setItem("tk", token);
        $.ajaxSetup({
            headers: { 'x-access-token': token }
        });
        return;
    }

    // send a message to feegow app if it's running on a webview to invalidate user token
    window.ReactNativeWebView?.postMessage(
        JSON.stringify({
            type: "loginFailed",
            payload: {
                licenseId: l,
            },
        })
    );
}

function replicarRegistro(id,tabela){
    $.post("ReplicarRegistros.asp", {id,tabela}, function(data){
        $("#importa-replicar").html(data);
        $('.multisel').multiselect({
            includeSelectAllOption: true,
            enableFiltering: true,
            numberDisplayed: 1,
        });
    });
}

const uploadProfilePic = async ({userId, db, table, content, contentType, elem = false}) => {
    let response = false;
    let endpoint = domain + "file/perfil/uploadPerfilFile";

    if (contentType === "form") {
        let objct = new FormData();
        objct.append('userType', table);
        objct.append('userId', userId);
        objct.append('licenca', db);
        objct.append('upload_file', content);
        objct.append('folder_name', "Perfil");

        response = await $.ajax({
            url: endpoint,
            type: 'POST',
            processData: false,
            contentType: false,
            data: objct,
            // Now you should be able to do this:
            mimeType: 'multipart/form-data',    //Property added in 1.5.1
        });

    }else{

        response = await jQuery.ajax({
            url: endpoint,
            type: 'post',
            dataType: 'json',
            data: JSON.stringify(content),
            beforeSend:function () {
                $('#divAvatar').show();
            }
        });

        $('#divAvatar').show();
        $('#divAvatar video').hide();
        $('#divDisplayFoto').css('display','block');
        $("#take-photo").hide();
        $("#cancelar").hide();

    }

    if (elem) {
        elem.attr("src", response.url);
    }

    return response;
}

const callRestApi = ({ params, method, path }) => {
    return fetch(domainApiRest + path, {
        "method": method,
        "headers": {
            "accept": "*/*",
            "content-type": "application/json; charset=UTF-8",
            "Authorization": `Bearer ${fgwGetStorageItem("tk")}`
        },
        "body": JSON.stringify(params)
    });
}
  
const recordLog = async (
    {
        category,
        licenseId,
        userId,
        oldData,
        newData,
        event
    }) => {

        if (!window.FEEGOW_ENVS || !window.FEEGOW_ENVS.FC_LOGS_SERVICE_URL) {
            return;
        }

        $.ajax({
            url: `${ window.FEEGOW_ENVS.FC_LOGS_SERVICE_URL}?tk=${fgwGetStorageItem("tk")}`,

            method: 'POST',
            dataType: 'json',
            contentType: 'application/json',
            data:
                JSON.stringify({
                    "licenseId": licenseId,
                    "type": "event",
                    "category": category,
                    "event": event,
                    "userId": userId,
                    "dateTime": (new Date()).toISOString(),
                    "payload": {
                        "oldData": oldData,
                        "newData": newData
                    },
                    "request": {
                        "url": window.location.href
                    }
                }),
            success:function(data) {
                
            },
            error: function (xhr, statustext, thrownError) {
               
            }
        });
    }

const doApiRequest = async (
    {
        url,
        params,
        method="get"
    }) => {


    return new Promise(function (resolve, reject) {
        $.get(api + url, params, function (data) {
            resolve({
                success: true,
                data: data,
                params: params
            });
        }).error(function (err) {
            reject({
                success: false,
                error: err
            })
        });
    })
};

/* FUNÇÕES DO AUTORIZADOR ONLINE */

function abrirModalToken(guiaID){
    openComponentsModal("/autorizador-online/modal-token", {microservico:'autorizadoronline', id:guiaID}, "Insira seu token", "lg", false)
}

function abrirSolicitacaoElegibilidade(pacienteID,convenioID,Referencia=""){
    solicitarElegibilidade("api/autorizador-online/solicitacao/solicitar-elegibilidade", {microservico:'autorizadoronline', pacienteID:pacienteID, convenioID:convenioID},Referencia)
}

function solicitarAutorizacao(url, params, type){
    if (url.indexOf(".asp") === -1) {
        d = domain;

        if (typeof(params) === 'object' && params.microservico)
        {
            d = getMicroserviceDomain(params.microservico)
        }
    }
    url = d + url;

	const token= fgwGetStorageItem("tk");

    var botaoOriginal = $("#btn-autorizar").clone();
	$.ajax({
        type: 'POST',
		url: url,
		data: params,
		headers: {
            "x-access-token":token
		},
        beforeSend: function () {
            closeComponentsModal();
            $("#span-autorizar").html("<i class='fas fa-sync-alt fa-spin'></i> Enviando...").removeClass("btn-danger").removeClass("btn-warning");
            $("#btn-autorizar").html("<i class='fas fa-sync-alt fa-spin'></i> Enviando...").removeClass("btn-danger").removeClass("btn-warning").addClass("btn-info");
            toggleBtnLoading("#btn-autorizar");
            showMessageDialog("Enviando dados à operadora...", "info");
        },
	}).done(function(data) {
        resultAutorizacao(data, type, params.updateGuia)
	}).fail(function(xhr, textStatus, error) {
        toggleBtnLoading("#btn-autorizar");
        $("#btn-autorizar").replaceWith(botaoOriginal.clone());
        showMessageDialog("<b> " + xhr.responseJSON.mensagem +" <br> ", "warning");
    });
}

function resultAutorizacao(data, tipoSolicitacao, updateGuia){
    if (typeof data === "string") {
        data = JSON.parse(data);
    }
    toggleBtnLoading("#btn-autorizar");
    let statusSolicitacao = "";
    
    if(tipoSolicitacao == "Cancelamento"){
        statusSolicitacao = data.body.status_solicitacao;
    }
    if(tipoSolicitacao == "ValidaToken"){
        statusSolicitacao = data.body.status;
    }
    if(tipoSolicitacao == "Autorizacao"){
        statusSolicitacao = data.body.status_solicitacao;
    }
    if (tipoSolicitacao == "Status") {
        statusSolicitacao = data.body.status_solicitacao || data.body.status;
    }

    statusSolicitacao = statusSolicitacao != "" && statusSolicitacao != null ? statusSolicitacao.toLowerCase() : "" ;

    let dataAtual = new Date();
    let title = "";
    let text = "Ocorreu uma resposta inesperada da operadora. Iremos atualizar a sua página, por favor verifique novamente o status da guia. Caso persista, por favor notificar ao suporte para analisarmos o caso.";
    let type = "danger";
    let delay = "4000";

    if(statusSolicitacao == "em análise" || statusSolicitacao == "em analise"){
        title = "Sua guia está em análise!";
        text =  "Para consultar o seu status, clique em Verificar Status. <br><br> A verificação de status é gratuita.";
        type = "warning";
        delay = "4000";
        $("#btn-autorizar, #span-autorizar").replaceWith("<span id='span-autorizar' style='border:none; color:#f6bb42; width:115px; height:22px;font-weight: 700;'><i class='fad fa-sync-alt'></i> Em Análise</span>");
    };

    if(statusSolicitacao == "aguardando justificativa tecnica do solicitante"){
        title = "Sua guia está em análise!";
        text =  '<b>Aguardando Justificativa Tecnica do Solicitante</b>.<br> Para que a guia possa ser autorizada, será necessário que o relatório médico com a indicação clínica seja enviado através do portal da operadora, acessando a opção "Guias em Auditoria". <br> Após envio, faça uma nova verificação de status.<br>Este procedimento é obrigatório, conforme exigência do convênio.';
        type = "warning";
        delay = "6000";
        $("#btn-autorizar, #span-autorizar").replaceWith("<span id='span-autorizar' style='border:none; color:#f6bb42; width:115px; height:22px;font-weight: 700;'><i class='fad fa-sync-alt'></i> Em Análise</span>");
    }

    if(statusSolicitacao == "negada"){
        title = "Os dados da guia indicam que ela não está autorizada.";
        text =  "Por favor, verifique os dados e tente novamente. <br><br> Caso ainda se encontre não autorizada, verifique dentro do portal da operadora.<br><br> Código: "+data.body.dados_procedimentos[0].motivos_negativa.codigo_glosa+"\n"+"Motivo: "+data.body.dados_procedimentos[0].motivos_negativa.descricao_glosa
        type = "danger";
        delay = "5000";
        $("#btn-autorizar, #span-autorizar").html("<i class='far fa-times-circle'></i> Autorização Negada").removeClass("btn-warning").addClass("btn-danger");
    }

    if(statusSolicitacao == "autorizada"){
        title = "Guia autorizada";
        text = ""
        type = "success";
        delay = "5000";
        $('#btn-autorizar, #span-autorizar').replaceWith("<span id='span-autorizar' style='border:none; color:green; width:115px; height:22px;font-weight: 600;'><i class='far fa-check-circle'></i> Guia Autorizada</span>");
    }

    if(statusSolicitacao == "cancelada com sucesso"){
        title = "Guia cancelada";
        text = "Sua guia foi cancelada."
        type = "success";
        delay = "5000";
        $('#btn-autorizar, #span-autorizar').replaceWith("<span id='span-autorizar' style='border:none; color:red; width:115px; height:22px;font-weight: 600;'><i class='far fa-check-circle'></i> Guia Cancelada </span>");
    }

    if(statusSolicitacao == "não cancelada" || statusSolicitacao == "guia inexistente"){
        title = "Guia não cancelada";
        text = "Sua guia não foi cancelada. <br><br> Código: "+data.body.motivos_negativa.codigo+"\n"+"Motivo: "+data.body.motivos_negativa.descricaoglosa;
        type = "warning";
        delay = "5000";
    }

    if(statusSolicitacao == "pendente_token"){
        title = "Guia Pendente Token";
        text = "Insira o token fornecido pela operadora para autorizar a guia"
        type = "warning";
        delay = "5000";
        $("#btn-autorizar").html("<i class='fas fa-engine-warning'></i> Token Pendente").removeClass("btn-danger").addClass("btn-warning");
        $("#btn-autorizar").attr("onclick", 'abrirModalToken('+data.GuiaID+')');
    }

    if(statusSolicitacao == "autorizado parcialmente"){
        title = "Guia Parcialmente Autorizada";
        text = "Consulte o portal da operadora para mais informações"
        type = "warning";
        delay = "5000";
        $("#btn-autorizar, #span-autorizar").html("<i class='far fa-times-circle'></i> Autorização Parcial").removeClass("btn-danger").addClass("btn-warning");
    }

    //Atualiza os campos da guia sadt
    if(updateGuia && (statusSolicitacao == "autorizada" || statusSolicitacao == "em analise" || statusSolicitacao == "em análise" || statusSolicitacao == "pendente_token"|| statusSolicitacao == "autorizado parcialmente") && (tipoSolicitacao == "Autorizacao")){
        let numeroGuiaOperadora = data.body.dados_autorizacao.numero_guia_operadora != "" ? data.body.dados_autorizacao.numero_guia_operadora : data.body.dados_autorizacao.numero_guia_operadora_solicitacao;
        let senha = data.body.dados_autorizacao.senha != "" ? data.body.dados_autorizacao.senha : data.body.dados_autorizacao.senha_guia_solicitacao;
        let validadeSenha = data.body.dados_autorizacao.data_validade_senha != "" ? data.body.dados_autorizacao.data_validade_senha : data.body.dados_autorizacao.validade_senha;
        $("#NGuiaOperadora").val(numeroGuiaOperadora);
        $("#Senha").val(senha);
        if(validadeSenha != ""){
            $("#DataValidadeSenha").val(moment(validadeSenha).format('DD/MM/YYYY'));
        }
        $("#DataAutorizacao").val(dataAtual.toLocaleDateString())
    }

    if(updateGuia && (statusSolicitacao == "autorizada" || statusSolicitacao == "em analise" || statusSolicitacao == "em análise") && tipoSolicitacao == "Status"){
        $("#NGuiaOperadora").val(data.body.numero_guia);
        if(data.body.senha != ''){
            $("#Senha").val(data.body.senha);
        }
        $("#DataAutorizacao").val(dataAtual.toLocaleDateString())
    }

    showMessageDialog(text, type, title, delay);

    if(title == ""){
        setTimeout(() => {
            location.reload(); //recarrega a página para pegar a nova autorizacao
        }, 3000);
    }
}

function verificaAutorizacao(GuiaID, TipoGuia, Servico) {
    $.get("getDadosServicosAutorizador.asp", {
        GuiaID, TipoGuia, Servico
    }, function (data) {
        if(data.length > 0) {
            let obj = JSON.parse(data);
            // if(obj.Autorizacao == 1){
                if(obj.status == "success"){
                    if(obj.statusSolicitacao == 1){
                        $('#btn-autorizar').replaceWith("<span id='span-autorizar' style='border:none; color:green; width:115px; height:22px;font-weight: 700;'><i class='far fa-check-circle'></i> Guia Autorizada</span>");
                    }else if(obj.statusSolicitacao == 6){
                        $("#btn-autorizar").replaceWith("<span id='span-autorizar' style='border:none; color:#f6bb42; width:115px; height:22px;font-weight: 700;'><i class='fad fa-sync-alt'></i> Em Análise</span>");
                    }else if(obj.statusSolicitacao == 3){
                        $("#btn-autorizar").html("<i class='far fa-times-circle'></i> Autorização Negada").removeClass("btn-warning").addClass("btn-danger");
                    }else if(obj.statusSolicitacao == 7){
                        $("#btn-autorizar").replaceWith("<span id='span-autorizar' style='border:none; color:red; width:115px; height:22px;font-weight: 700;'><i class='far fa-check-circle'></i> Guia Cancelada</span>");
                    }else if(obj.statusSolicitacao == 8){
                        $("#btn-autorizar").html("<i class='fas fa-engine-warning'></i> Token Pendente").removeClass("btn-danger").addClass("btn-warning");
                        $("#btn-autorizar").attr("onclick", 'abrirModalToken('+GuiaID+')');
                    }
                    else if(obj.statusSolicitacao == 2){
                        $("#btn-autorizar").html("<i class='far fa-times-circle'></i> Parcialmente Autorizada").removeClass("btn-danger").addClass("btn-warning");
                    }
                }
        }
    });
}

// autorizador online solicitar elegibilidade
function solicitarElegibilidade(url, params, ref=""){
    if (url.indexOf(".asp") === -1) {
        d = domain;

            if (typeof(params) === 'object' && params.microservico)
            {
            d = getMicroserviceDomain(params.microservico)
            }
    }
    url = d + url;

	const token= fgwGetStorageItem("tk");

    dados = {'PacienteID': params.pacienteID, 'ConvenioID': params.convenioID}

	$.ajax({
		type: 'POST',
		url: url,
		data: JSON.stringify(dados),
		headers: {
            "Content-Type": "application/json",
            "Accept": "application/json",
			"x-access-token":token
		},
        beforeSend: function () {
            $("#btnElegibilidade"+ref).html("<i class='fas fa-sync fa-spin'></i> Solicitando...");
            toggleBtnLoading("#btnElegibilidade"+ref);
            showMessageDialog("Enviando dados à operadora...", "info");
        },
	}).done(function(data) {
        toggleBtnLoading("#btnElegibilidade"+ref);

        if(data.body.eligible){
            showMessageDialog("Elegível", "success");
            $("#sectionElegibilidade"+ref).html("<div style='border:none; color:green; width:115px; height:22px;'><i class='far fa-check-circle'></i> Paciente Elegível </div> <label class='control-label'><i>Última Verificação: "+getCurrentDate('dd/mm/yy')+"</i></label>");
        }else{
            if(data.status == 200){
                $("#btnElegibilidade"+ref).removeClass('btn-warning').addClass('btn-danger').html("<i class='far fa-times-circle'></i> Paciente Inelegível");
            }else{
                $("#btnElegibilidade"+ref).html("<i class='far fa-engine-warning'></i> Erro na Solicitação");
                showMessageDialog("Ocorreu um erro ao solicitar a elegibilidade deste paciente. </br></br></br> <b>Motivo do erro:</b> "+ data.body.message.message + " </br></br></br> Por favor, tente novamente mais tarde ou contate o suporte.", "warning");
            }
        }
	}).fail(function(xhr, textStatus, error) {
        toggleBtnLoading("#btnElegibilidade"+ref);
        $("#btnElegibilidade"+ref).html("<i class='far fa-engine-warning'></i> Erro na Solicitação");
        showMessageDialog("<b>Ocorreu um erro interno:</b> <br> por favor, entre em contato com o suporte. <br> <b>error:</b> " + error , "warning");
    });
}

// Obtendo a data atual no formato desejado
function getCurrentDate(formato) {

    var dataAtual = new Date();
    var dia = String(dataAtual.getDate()).padStart(2, '0');
    var mes = String(dataAtual.getMonth() + 1).padStart(2, '0');
    var ano = String(dataAtual.getFullYear());

    if (formato === 'dd/mm/yyyy') {
        return `${dia}/${mes}/${ano}`;
    } else if (formato === 'dd/mm/yy') {
        ano = ano.slice(-2);
        return `${dia}/${mes}/${ano}`;
    } else if (formato === 'yyyy/mm/dd') {
        return `${ano}/${mes}/${dia}`;
    }
    return null;
}
// teste pendente de aprovação
function abrirModalDivulgacao(){
    openComponentsModal("/autorizador-online/divulgacao/", {microservico:'autorizadoronline'}, "Assine Já", false, false)
}



/* FUNÇÕES DA INTEGRAÇÃO LABORATORIAL */
function abrirIntegracaov2(tabela, id, labid, config) {
    const params = {
        microservico: 'integracaolaboratorial',
        id: id,
        labid: labid,
        tabela: tabela
    };

    if (typeof config === 'number' && config === 1) {
        params.config = {
            labWatson: true
        };
    }

    openComponentsModal(
        "labs-integration/modal-integracao", 
        params, 
        false, 
        false
    );
}

function abrirSelecaoLaboratorio(vartabela, varid, versao, config){
    if (versao !='2')
    {
        openComponentsModal("labs-integration/invoice-lab-select", {invoiceId: varid, itens:0 }, "Integração com Laboratórios", false, false)
    } else {
        const params = {
            microservico: 'integracaolaboratorial',
            tabela: vartabela,
            id: varid,
        };

        if (typeof config === 'number' && config === 1) {
            params.config = {
                labWatson: true
            };
        }
        
        openComponentsModal(
            "labs-integration/modal-lab-select",
            params,
            '',
            false,
            false,
            'md'
        );
    }
}

function abrirSolicitacao(varid, versao, labid, config){    
    if (versao !='2')
    {
       // openComponentsModal("labs-integration/modal-detalhes-solicitacao", {microservico:'x', id: varid },'',false,false,'md');
       abrirIntegracao(varid,labid,0)
    }
    else
    {
        const params = {
            microservico: 'integracaolaboratorial',
            id: varid
        };
        if (typeof config === 'number' && config === 1) {
            params.config = {
                labWatson: true
            };
        }
        openComponentsModal("labs-integration/modal-detalhes-solicitacao", params, '', false, false, 'md');
    }
}
/* FUNÇÃO MANTIDA PARA COMPATIBILIDADE COM A VERSÃO 1 */
function selecionaLaboratorio() {
    var labid  = $('#selectLabID :selected').val();
    var invoiceid = $('#varinvoiceid').val();
    var itensCount = $('#varitenscount').val();

    abrirIntegracao(invoiceid,labid,itensCount);
    
}
/* FUNÇÃO MANTIDA PARA COMPATIBILIDADE COM A VERSÃO 1 */
function abrirIntegracao(invoiceId,labid,itenscount) {
    switch (labid.trim()) {        
        case '1':
            openComponentsModal("labs-integration/matrix/invoice-exams", {invoiceId: invoiceId, labid:labid }, false, false);
            break;
        case '2':
            openComponentsModal("labs-integration/diagbrasil/invoice-exams", {invoiceId: invoiceId, labid:labid, itens:itenscount }, false, false);
            break;
        case '3':
            openComponentsModal("labs-integration/alvaro/invoice-exams", {invoiceId: invoiceId, labid:labid, itens:itenscount }, false, false);
            break;
        case '4':
            openComponentsModal("labs-integration/hermespardini/invoice-exams", {invoiceId: invoiceId, labid:labid, itens:itenscount }, false, false);
            break;
        case '5':
            openComponentsModal("labs-integration/shift/invoice-exams", {invoiceId: invoiceId, labid:labid, itens:itenscount }, false, false);
            break;
        default:
            alert ('Código de Laboratório não implementado');
        }
}


/* FIM DAS FUNÇÕES DA INTEGRACAO LABORATORIAL */

const toggleBtnLoading = (btnSelector) => {
    const $el = $(btnSelector);
    const $elIcon = $el.find("i");
    let isLoading = $el.data("data-loading") === "true";

    if(isLoading){
        $el.attr("disabled", false);
        $el.data("data-loading", "false");
        $elIcon.removeClass("fa-circle-o-notch fa-spin");
    }else{
        $el.data("data-loading", "true");
        $el.attr("disabled", true);
        $elIcon.addClass("fa-circle-o-notch fa-spin");
    }
}

const spawnDebugWindow = () => {
    const $win = `
    <style>
        .debug-panel {
            width: 400px;
            height: 200px;
            background: #000000d1;
            position: fixed;
            bottom: 50px;
            right: 20px;
            border-radius: 6px;
            -webkit-box-shadow: 0 3px 18px rgb(0 0 0 / 10%);
            box-shadow: 0 3px 18px rgb(0 0 0 / 10%);
            background-clip: padding-box;
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            overflow-y: scroll;
            overflow-x: hidden;
            font-size:11px;
        }
        .debug-panel .debug-list {
            padding: 10px;
        }
        .debug-panel .debug-item{
            color: #00ba00;
            font-family: monospace;
            list-style-type: none;
        }
        .debug-item pre {
            color: #00ba00;
            font-size:11px;
            font-family: monospace;
            background: #00000054;
            border: 1px;
        }
    </style>
    <div class="debug-panel">
        <ul class="debug-list">

        </ul>
    </div>`;

    $("#main").append($win);
}

const debugMessage = ({
    message,
    obj,
    type = 'INFO'
}) => {
    const $debugWindow = $(".debug-panel");
    if($debugWindow.length > 0){
        let objString = obj;

        if(typeof objString === "string"){
            objString = JSON.parse(objString);
        }
        objString = JSON.stringify(objString, null, '\t')

        const $debugItem = `
        <li class="debug-item">
            $ ${type} - ${message}:
            <pre>${objString}</pre>
        </li>`;

        console.log(`${type} - ${message}:`);
        if(obj){            
            console.log(obj);
        }

        $debugWindow.find(".debug-list").append($debugItem);
        
    }
}



function openApiIframeModal(url) {
    const tk = fgwGetStorageItem("tk");
    if (url.indexOf(domain) === -1) {
        url = domain + url;
    }
    if (url.indexOf("tk=") === -1) {
        url += (url.indexOf("?") === -1 ? "?" : "&") + "tk=" + tk;
    }

    var loadingContainerHtml = `<style>@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }</style>
        <div class="loading-container" style="display:flex; flex-direction:column; justify-content:center; align-items:center; height:100%;">
        <div class="spinner" style="border: 5px solid #f3f3f3; border-top: 5px solid #3498db; border-radius: 50%; width: 50px; height: 50px; animation: spin 2s linear infinite;"></div>
        <div class="spiner" style="margin-top: 5px;">Iniciando...</div>
        <div class="delay-message" style="display:none; text-align:center; color: #555; margin-top: 20px;">Isso está demorando mais que o normal, gostaria de cancelar?</div>
    </div>`;    
    
    // Cria o modal e adiciona o contêiner de carregamento
    var iframeModal = getModal(true, "lg", "80%");
    iframeModal              
               .modal({backdrop: 'static', keyboard: false})
               .find('.fgw-modal-content')
               .css("height", "90vh")
               .css("padding", "15")
               .css("background-color", "#fff")
               .html(loadingContainerHtml);

    var closeBtn = $('<button>', {
        html: '<i class="far fa-times"></i>',
        css: {
            position: 'absolute',
            top: '-10px',
            right: '-10px',
            'font-size': '20px',
            'border-radius': '50%',
            'z-index': '9999'
        },
        id: 'btn-close-iframe',
        class: 'btn btn-lg btn-secondary',
    });
    iframeModal.find('.fgw-modal-content').append(closeBtn);

    // Configura o iframe para carregar o conteúdo
    setTimeout(function () {
        var iframe = $("<iframe>", {
            src: url,
            css: {
                width: "99%",
                height: "100%",
                border: "none",
                scrolling: "no"
            },
            on: {
                load: function () {
                    iframeModal.find('.loading-container').hide();
                    clearTimeout(delayTimeout);
                    window.addEventListener("message", function(event) {
                        if (event.data === 'closeApiModal') {
                            $('#btn-close-iframe').trigger('click');
                        }
                    }, false);
                }
            }
        });
        
        iframeModal.find('.fgw-modal-content').append(iframe);
    }, 100);

    // Configura o temporizador para exibir a mensagem de atraso
    var delayTimeout = setTimeout(function () {
        iframeModal.find('.delay-message').css("display", "block");
        var cancelButton = $('<button>', {
            text: 'Cancelar',
            css: {
                display: 'block',
                margin: '10px auto'
            },
            class: 'btn btn-sm btn-secondary',
            click: function () {
                iframeModal.modal('hide');
            }
        });
        iframeModal.find('.loading-container').append(cancelButton);
    }, 20000);
}



function openApiModalPdfPreview(url) {
    const tk = fgwGetStorageItem("tk");
    if (url.indexOf(domain) === -1) {
        url = domain + url;
    }
    if (url.indexOf("tk=") === -1) {
        url += (url.indexOf("?") === -1 ? "?" : "&") + "tk=" + tk;
    }

    var loadingContainerHtml = `<style>@keyframes spin { 0% { transform: rotate(0deg); } 100% { transform: rotate(360deg); } }</style>
        <div class="loading-container" style="display:flex; flex-direction:column; justify-content:center; align-items:center; height:100%;">
        <div class="spinner" style="border: 5px solid #f3f3f3; border-top: 5px solid #3498db; border-radius: 50%; width: 50px; height: 50px; animation: spin 2s linear infinite;"></div>
        <div class="delay-message" style="display:none; text-align:center; color: #555; margin-top: 20px;">Isso está demorando mais que o normal, gostaria de cancelar?</div>
    </div>`;    
    
    // Cria o modal e adiciona o contêiner de carregamento
    var pdfModal = getModal(true, "lg", "80%");
    pdfModal
        .modal()
        .find('.fgw-modal-content')
        .css("height", "80vh")
        .css("padding", "15px")
        .css("text-align", "center")
        .html(loadingContainerHtml);

    // Faz a requisição GET para a API e exibe o PDF
    fetch(url)
        .then(response => {
            if (response.ok) return response.blob();
            throw new Error('Network response was not ok.');
        })
        .then(blob => {
            var pdfUrl = URL.createObjectURL(blob);
            var objectEl = $('<object type="application/pdf" style="width: 100%; height:calc(100% - 50px);">');
            objectEl.attr('data', pdfUrl);
            pdfModal.find('.fgw-modal-content').html(objectEl);
            pdfModal.find('.loading-container').remove();
            var signButton = $('<button>', {
                html: '<i class="far fa-play"></i> Iniciar Assinatura',
                css: {
                    width: '100%',
                },
                class: 'btn btn-lg btn-success',
                click: function () {
                    //@todo: implementar disaro do processo de assinatura (rota: pre-batch-sign eu acho)
                }
            });
            pdfModal.find('.fgw-modal-content').append(signButton);
        })
        .catch(error => {
            console.error('Error fetching PDF:', error);
            pdfModal.find('.loading-container').hide();
            var errorMessage = $('<div>').text('Erro ao carregar o PDF. Por favor, tente novamente mais tarde.').css("text-align", "center").css("color", "red");
            pdfModal.find('.fgw-modal-content').html(errorMessage);
        });

    // Configura o temporizador para exibir a mensagem de atraso
    var delayTimeout = setTimeout(function () {
        pdfModal.find('.delay-message').css("display", "block");
        var cancelButton = $('<button>', {
            text: 'Cancelar',
            css: {
                display: 'block',
                margin: '10px auto'
            },
            class: 'btn btn-sm btn-secondary',
            click: function () {
                pdfModal.modal('hide');
            }
        });
        pdfModal.find('.loading-container').append(cancelButton);
    }, 50000);
}

async function includeFeegowFrontEndModule(moduleName, loadCss = true, loadChunks = true){
    if(!feegowFrontendURL.endsWith("/")){
        feegowFrontendURL = feegowFrontendURL + "/";
    } 
    return  new Promise(async (resolve, reject) => {
        try {
            if (loadCss && env !== 'local') {
                const cssUrl = `${feegowFrontendURL}css/${moduleName}.css?t=${cacheDate}`;
                await IncludeCSS(moduleName, cssUrl)
            }

            if (loadChunks) {
                const chunkVendorCssUrl = `${feegowFrontendURL}css/chunk-vendors.css?t=${cacheDate}`;
                const chunkVendorsUrl = `${feegowFrontendURL}js/chunk-vendors.js?t=${cacheDate}`;
                const chunkCommonUrl = `${feegowFrontendURL}js/chunk-common.js?t=${cacheDate}`;
                if (env !== 'local') {
                    await IncludeCSS("chunk-vendors", chunkVendorCssUrl)
                }
                await IncludeScript("chunk-vendors",chunkVendorsUrl)
                await IncludeScript("chunk-common",chunkCommonUrl)
            }

            const moduleUrl = `${feegowFrontendURL}js/${moduleName}.js?t=${cacheDate}`;
            await IncludeScript(moduleName,moduleUrl)
            resolve()
        } catch (error) {
            reject(error);
        }
    })
}

async function IncludeScript(id,url){
    return new Promise(async (resolve, reject) => {
        if (!isScriptLoaded(url)) {
            const newScript = document.createElement("script");
            newScript.src = url;
            newScript.defer = true;
            newScript.onload = () => {
                console.log("script load: "+id)
                resolve();
              };
            newScript.onerror = (error) => {
                console.log("script reject: "+id)
                resolve()
            };
            document.head.appendChild(newScript);
        } else {
            console.log("Script já carregado: " + url);
            resolve()
        }
    })
}

async function IncludeCSS(id,url){
    return new Promise(async (resolve, reject) => {
        if (!isCssLoaded(url)) {
            const link = document.createElement("link");
            link.href = url;
            link.rel = "stylesheet";
            link.onload = () => {
                console.log("link resolve: "+id)
                resolve();
            };
            link.onerror = (error) => {
                console.log("link reject: "+id)
                resolve();
            };
            document.head.appendChild(link);
        } else {
            console.log("CSS já carregado: " + id);
            resolve()
        }
    })
}

function isScriptLoaded(url) {
    const scripts = document.getElementsByTagName('script');
    for (let i = 0; i < scripts.length; i++) {
        if (scripts[i].src.includes(`${url}`)) {
            return true;
        }
    }
    return false;
}

function isCssLoaded(url) {
    const links = document.getElementsByTagName('link');
    for (let i = 0; i < links.length; i++) {
        if (links[i].href.includes(`${url}`)) {
            return true;
        }
    }
    return false;
}

const setOpen = (id, idSub) => {
    const di = document.querySelectorAll("tr");
    const arow = document.getElementById(id);
    if(arow.className === "fa fa-angle-up"){
        arow.className = "fa fa-angle-down"
    }else{
        arow.className = "fa fa-angle-up"
    }

    di.forEach((element) => {
        if(element.id == idSub){
            element.style.display = (element.style.display === "none") ? "table-row" : "none";
        }
    });

}

async function autoConsolidate(invoiceId, autoConsolidarBool = 1, callback = false, routePrefix = "") {
    var d = new Date();
    var hours = d.getHours();
    var minutes = d.getMinutes();
    var seconds = d.getSeconds();
    var time = hours + ":" + minutes + ":" + seconds;

    $("#AutoConsolidar").attr("src",routePrefix.toString() +"AutoConsolidar.asp?ProcedimentoID=0&T=" + time+"&PagarInvoiceID="+invoiceId+"&ExibirExecutadoOuNao=S&ExecutarRepasseConsolida=S");

    return true

}
